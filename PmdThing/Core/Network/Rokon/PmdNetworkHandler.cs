using PmdThing.Core.Util;
using System.Reflection;
using static PmdThing.Core.Network.Rokon.PmdPacket;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Core.Network.Rokon
{
    public abstract class PmdNetworkHandler(PmdComponentConfig config, Type syncAttribute, PmdPacketSource packetSource) : PmdComponent(config)
    {

        public static PmdNetworkHandler? Instance { get; protected set; }

        protected record ContainerEntry(PmdContainerId Id, object[]? Arguments);

        protected record TagEntry(string Tag, PmdContainerId ParentId);


        // This is because containers don't store their IDs. I want to change this but cba rn but this is inefficient
        // TODO: the client doesn't need it so separate this out somehow
        protected readonly Dictionary<PmdContainer, ContainerEntry> ContainerEntries = [];
        private readonly Dictionary<PmdComponentId, TagEntry> _idTags = [];
        private readonly Dictionary<TagEntry, PmdComponentId> _tagIds = [];

        protected class ListenableHandler<T>(PmdNetworkHandler listener, PacketDto packetDto) where T : notnull
        {
            // ReSharper disable once UnusedMember.Global
            public void Changed(T current, T previous)
            {
                listener.ListenableValueChanged(current, previous, packetDto);
            }
        }

        public void RegisterSyncedComponent(PmdComponentId id, PmdContainer parent, string tag)
        {
            if (ContainerEntries.TryGetValue(parent, out var entry))
            {
                _idTags.Add(id, new TagEntry(tag, entry.Id));
                _tagIds.Add(new TagEntry(tag, entry.Id), id);
            }
        }

        public PmdComponentId GetIdForTag(string tag, PmdContainerId parentId)
        {
            return _tagIds[new TagEntry(tag, parentId)];
        }

        public string GetTagForId(PmdComponentId id)
        {
            return _idTags[id].Tag;
        }

        public abstract void ListenableValueChanged(dynamic current, dynamic previous, PacketDto packetDto);

        public void RegisterComponent(PmdComponentId id, PmdComponent component, bool unique)
        {
            var componentType = component.GetType();

            // listenable values
            foreach (var field in componentType.GetFields())
            {
                var fieldValue = field.GetValue(component);

                if (fieldValue == null) continue;
                if (!IsGenericClass(field.FieldType, typeof(PmdListenableValue<>))) continue;
                if (!field.IsDefined(syncAttribute)) continue;

                var tag = _idTags.GetValueOrDefault(id);
                if (tag == null) throw new InvalidOperationException("Tag for parent component not found. Register only using RegisterSyncedComponent.");

                var parent = component.GetParent();
                var parentId = parent != null ? ContainerEntries[parent].Id : (PmdContainerId?)null;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Attribute = field.Name,
                    DataType = field.FieldType.GenericTypeArguments[0],
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    Unique = unique,
                    Source = packetSource,
                    ParentContainer = parentId,
                };

                var changedEvent = field.FieldType.GetEvent("Changed");
                if (changedEvent == null) continue;

                SubscribeEvent(changedEvent, packetDto, fieldValue);
            }

            // check if it's a listenable component
            if (IsGenericClass(componentType, typeof(PmdListenableComponent<>)))
            {
                var tag = _idTags.GetValueOrDefault(id);
                if (tag == null) throw new InvalidOperationException("Tag for listenable component not found. Register only using RegisterSyncedComponent.");

                if (!componentType.IsDefined(syncAttribute)) return;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Unique = unique,
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    DataType = componentType.BaseType?.GenericTypeArguments[0],
                    Source = packetSource,
                    Attribute = tag
                };

                var changedEvent = componentType.GetEvent("Changed");
                if (changedEvent == null) return;

                SubscribeEvent(changedEvent, packetDto, component);
            }
        }

        protected static bool IsGenericClass(Type type, Type genericType)
        {
            if (type.BaseType?.IsGenericType == true)
            {
                return type.BaseType?.GetGenericTypeDefinition() == genericType;
            }
            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition() == genericType;
            }

            return false;
        }

        private void SubscribeEvent(EventInfo info, PacketDto packetDto, object target)
        {
            var genericHandler = typeof(ListenableHandler<>);
            var dataType = packetDto.DataType;
            var dataHandler = genericHandler.MakeGenericType(dataType!);

            var handlerMethod = dataHandler.GetMethod("Changed", BindingFlags.Public | BindingFlags.Instance);

            var handlerWrapper = Activator.CreateInstance(dataHandler, this, packetDto);
            var handlerDelegate =
                Delegate.CreateDelegate(info.EventHandlerType!, handlerWrapper, handlerMethod!);

            info.AddEventHandler(target, handlerDelegate);
        }
    }
}
