using PmdThing.Core.Util;
using System.Reflection;
using static PmdThing.Core.Network.Rokon.PmdPacket;
using PmdComponentId = System.Int32;

namespace PmdThing.Core.Network.Rokon
{
    public abstract class PmdNetworkComponent(PmdComponentConfig config, Type syncAttribute, PmdPacketSource packetSource) : PmdComponent(config)
    {
        protected class ListenableHandler<T>(PmdNetworkComponent listener, PacketDto packetDto) where T : notnull
        {
            // ReSharper disable once UnusedMember.Global
            public void Changed(T current, T previous)
            {
                listener.ListenableValueChanged(current, previous, packetDto);
            }
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

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Attribute = field.Name,
                    DataType = field.FieldType.GenericTypeArguments[0],
                    PmdPacketType = PmdPacketType.UpdateComponent,
                    Unique = unique,
                    Source = packetSource
                };

                var changedEvent = field.FieldType.GetEvent("Changed");
                if (changedEvent == null) continue;

                SubscribeEvent(changedEvent, packetDto, fieldValue);
            }

            // check if it's a listenable component
            if (IsGenericClass(componentType, typeof(PmdListenableComponent<>)))
            {
                if (!componentType.IsDefined(syncAttribute)) return;

                var packetDto = new PacketDto
                {
                    Id = id,
                    Type = componentType,
                    Unique = unique,
                    PmdPacketType = PmdPacketType.AddComponent,
                    DataType = componentType.BaseType?.GenericTypeArguments[0],
                    Source = packetSource
                };

                var changedEvent = componentType.GetEvent("Changed");
                if (changedEvent == null) return;

                SubscribeEvent(changedEvent, packetDto, component);
            }
        }

        private static bool IsGenericClass(Type type, Type genericType)
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
