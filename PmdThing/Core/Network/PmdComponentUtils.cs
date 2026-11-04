using PmdThing.Components.Networking;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.ComponentModel;
using System.Reflection;
using static PmdThing.Core.Network.Rokon.PmdPacket;
using PmdComponentId = System.Int32;

namespace PmdThing.Core.Network
{
    public class PmdComponentUtils
    {

        /// <summary>
        /// Wrapper around event with added component info
        /// </summary>
        protected class ListenableHandler(PmdNetworkComponent listener, PmdPacket.PacketDto packetDto)
        {
            // ReSharper disable once UnusedMember.Global
            public void Changed(object current, object previous)
            {
                listener.ListenableValueChanged(current, previous, packetDto);
            }
        }

        public abstract class PmdNetworkComponent(PmdComponentConfig config) : PmdComponent(config)
        {
            public abstract void ListenableValueChanged(object current, object previous, PacketDto packetDto);

            public void RegisterComponent(PmdComponentId id, PmdComponent component, bool unique)
            {
                var componentType = component.GetType();

                var packetDto = new PmdPacket.PacketDto
                {
                    Id = id,
                    Type = componentType,
                    PacketType = PacketType.AddComponent,
                    Unique = unique
                };

                // listenable values
                foreach (var field in componentType.GetFields())
                {
                    if (field.FieldType != typeof(PmdListenableValue<>)) continue;
                    if (!field.IsDefined(typeof(SyncedComponent))) continue;

                    var changedEvent = field.FieldType.GetEvent("Changed");
                    if (changedEvent == null) continue;

                    packetDto.Attribute = field.Name;
                    packetDto.DataType = field.FieldType.GenericTypeArguments[0];

                    SubscribeEvent(changedEvent, packetDto, component);
                }

                // check if it's a listenable component
                if (componentType == typeof(PmdListenableComponent<>))
                {
                    if (!componentType.IsDefined(typeof(SyncedComponent))) return;

                    var changedEvent = componentType.GetEvent("Changed");
                    if (changedEvent == null) return;

                    SubscribeEvent(changedEvent, packetDto, component);
                }
            }

            private void SubscribeEvent(EventInfo info, PacketDto packetDto, object target)
            {
                var handlerMethod =
                    typeof(ListenableHandler).GetMethod("Changed", BindingFlags.Public | BindingFlags.Instance);

                var handlerWrapper = new ListenableHandler(this, packetDto);
                var handlerDelegate =
                    Delegate.CreateDelegate(info.EventHandlerType!, handlerWrapper, handlerMethod!);

                info.AddEventHandler(target, handlerDelegate);
            }
        }


    }
}
