using PmdThing.Core.Util;
using System.Reflection;
using static PmdThing.Core.Network.Rokon.PmdPacket;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Core.Network.Rokon
{

    public abstract class PmdNetworkHandler(PmdContainerConfig config, Type syncAttribute, PmdPacketSource packetSource) : PmdContainer(config)
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

        public virtual void RegisterSyncedComponent(PmdComponentId id, PmdComponent component, bool unique, PmdContainer parent, string tag)
        {
            _idTags.Add(id, new TagEntry(tag, parent.Id));
            _tagIds.Add(new TagEntry(tag, parent.Id), id);

            var componentType = component.GetType();

            // listenable values
            foreach (var field in componentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldValue = field.GetValue(component);

                if (fieldValue == null) continue;
                if (!PmdTypeUtils.IsGenericClass(field.FieldType, typeof(PmdServerValue<>))) continue;
                //if (!field.IsDefined(syncAttribute)) continue;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Attribute = field.Name,
                    DataType = field.FieldType.GenericTypeArguments[0],
                    PmdPacketType = PmdPacketType.UpdateValue,
                    Unique = unique,
                    Source = packetSource,
                    Parent = parent.Id,
                    Tag = tag
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
                var syncAttribInstance = Attribute.GetCustomAttribute(componentType, typeof(PmdServerSync)) as PmdServerSync;
                
                if (syncAttribInstance?.UpdateValue == false) return;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Unique = unique,
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    DataType = componentType.BaseType?.GenericTypeArguments[0],
                    Source = packetSource,
                    Tag = tagEntry.Tag,
                    Parent = parent.Id
                };

                var changedEvent = componentType.GetEvent("Changed");
                if (changedEvent == null) return;

                SubscribeEvent(changedEvent, packetDto, component);

            }
        }

        private void RegisterValue(Type valueType, object instance)
        {

        }

        public PmdComponentId? GetIdForTag(string tag, PmdContainerId parentId)
        {
            if (_tagIds.TryGetValue(new TagEntry(tag, parentId), out var id))
            {
                return id;
            }
            return null;
        }

        public string? GetTagForId(PmdComponentId id)
        {
            return _idTags[id]?.Tag;
        }

        /// <summary>
        /// When a listenable field / component changes.
        /// </summary>
        /// <param name="current">Current value</param>
        /// <param name="previous">Previous value</param>
        /// <param name="packetDto">Built packet to send</param>
        public abstract void ListenableValueChanged(dynamic current, dynamic previous, PacketDto packetDto);

        private void SubscribeEvent(EventInfo info, PacketDto packetDto, object target)
        {
            var dataHandler = typeof(ListenableHandler<>).MakeGenericType(packetDto.DataType!);

            var handlerMethod = dataHandler.GetMethod("Changed", BindingFlags.Public | BindingFlags.Instance);

            var handlerWrapper = Activator.CreateInstance(dataHandler, this, packetDto);
            var handlerDelegate =
                Delegate.CreateDelegate(info.EventHandlerType!, handlerWrapper, handlerMethod!);

            info.AddEventHandler(target, handlerDelegate);
        }
    }
}
