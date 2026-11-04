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

        protected record TagEntry(string Tag, PmdContainerId ParentId);

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

        public void RegisterSyncedComponent(PmdComponentId id, PmdComponent component, bool unique, PmdContainer parent, string tag)
        {
            _idTags.Add(id, new TagEntry(tag, parent.Id));
            _tagIds.Add(new TagEntry(tag, parent.Id), id);

            var componentType = component.GetType();

            // listenable values
            foreach (var field in componentType.GetFields())
            {
                var fieldValue = field.GetValue(component);

                if (fieldValue == null) continue;
                if (!PmdTypeUtils.IsGenericClass(field.FieldType, typeof(PmdListenableValue<>))) continue;
                if (!field.IsDefined(syncAttribute)) continue;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Attribute = field.Name,
                    DataType = field.FieldType.GenericTypeArguments[0],
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    Unique = unique,
                    Source = packetSource,
                    ParentContainer = parent.Id
                };

                var changedEvent = field.FieldType.GetEvent("Changed");
                if (changedEvent == null) continue;

                SubscribeEvent(changedEvent, packetDto, fieldValue);
            }

            // check if it's a listenable component
            if (PmdTypeUtils.IsGenericClass(componentType, typeof(PmdListenableComponent<>)))
            {
                var tagEntry = _idTags.GetValueOrDefault(id);
                if (tagEntry == null) throw new InvalidOperationException("Tag for listenable component not found. Register only using RegisterSyncedComponent.");

                if (!componentType.IsDefined(syncAttribute)) return;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Unique = unique,
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    DataType = componentType.BaseType?.GenericTypeArguments[0],
                    Source = packetSource,
                    Attribute = tagEntry.Tag
                };

                var changedEvent = componentType.GetEvent("Changed");
                if (changedEvent == null) return;

                SubscribeEvent(changedEvent, packetDto, component);
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
