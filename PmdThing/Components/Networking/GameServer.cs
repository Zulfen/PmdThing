using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using PmdComponentId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class GameServer<TAddr, TConn> : PmdComponent where TAddr : notnull where TConn : ServerConnection
    {

        protected class ListenableHandler(GameServer<TAddr, TConn> server, PmdPacket.PacketDto packetDto)
        {
            public void Changed(object current, object previous)
            {
                server.ListenableValueChanged(current, previous, packetDto);
            }
        }

        public event Action<TAddr>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        protected readonly Dictionary<TAddr, TConn> Connections = [];

        private readonly BlockingCollection<PmdPacket.PacketDto> _globalQueue = [];
        private readonly CancellationTokenSource _shutdown = new();

        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdComponentConfig config) : base(config)
        {
            ClientConnected += GameServer_ClientConnected;
            ClientDisconnected += GameServer_ClientDisconnected;
            Base.ComponentRegistered += RegisterComponent;
        }

        protected abstract void SendBytes(TConn connection, byte[] bytes);
        protected abstract byte[] ReceiveBytes(TConn connection);

        public void RegisterComponent(PmdComponentId id, PmdComponent component, bool unique)
        {
            var componentType = component.GetType();

            var packetDto = new PmdPacket.PacketDto
            {
                Id = id,
                Type = componentType,
                PacketType = PacketType.Add,
                Unique = unique
            };

            var handlerMethod = typeof(ListenableHandler).GetMethod("Changed", BindingFlags.Public | BindingFlags.Instance);

            // listenable values
            foreach (var field in componentType.GetFields())
            {
                if (field.FieldType == typeof(PmdListenableValue<>)) continue;
                if (!field.IsDefined(typeof(PmdNetworkedAttribute))) continue;

                var changedEvent = field.FieldType.GetEvent("Changed");
                if (changedEvent == null) continue;

                packetDto.Field = field.Name;
                var handlerWrapper = new ListenableHandler(this, packetDto);
                var handlerDelegate = Delegate.CreateDelegate(changedEvent.EventHandlerType!, handlerWrapper, handlerMethod!);

                changedEvent.AddEventHandler(component, handlerDelegate);
            }

            // check if it's a listenable component
            if (componentType == typeof(PmdListenableComponent<>))
            {
                if (!componentType.IsDefined(typeof(PmdNetworkedAttribute))) return;

                var changedEvent = componentType.GetEvent("Changed");
                if (changedEvent == null) return;

                var handlerWrapper = new ListenableHandler(this, packetDto);
                var handlerDelegate = Delegate.CreateDelegate(changedEvent.EventHandlerType!, handlerWrapper, handlerMethod!);

                changedEvent.AddEventHandler(component, handlerDelegate);
            }
        }

        protected void GameServer_ClientConnected(TAddr address)
        {
            Register<TConn>(true, out var connection, address);
            Connections.Add(address, connection);
        }

        protected void GameServer_ClientDisconnected(TAddr address)
        {
            Connections.Remove(address, out var connection);
            connection?.Cleanup();
        }

        private void UpdateListenableValue(PmdComponentId componentId, string fieldName, object data)
        {
            var component = GetOtherComponent(componentId);

            var setMethod = component.GetType()
                .GetField(fieldName)?
                .GetValue(component)?
                .GetType()
                .GetMethod("Set");

            setMethod?.Invoke(component, [data]);
        }

        private void UpdateListenableComponent(PmdComponentId componentId, object data)
        {
            var component = GetOtherComponent(componentId);
            var value = component.GetType().GetProperty("ComponentValue");

            value?.SetValue(component, data);
        }

        private void ListenableValueChanged(object current, object previous, PmdPacket.PacketDto packetDto)
        {
            if (current.Equals(previous)) return;

            packetDto.Data = [current];
            _globalQueue.Add(packetDto, _shutdown.Token);
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void ReceiveLoop(TConn connection)
        {
            while (Running)
            {
                var packetBytes = ReceiveBytes(connection);
                try
                {
                    var packet = PmdPacket.Decode(packetBytes);
                    if (!PmdPacket.ValidPacket(packet)) continue;

                    switch (packet.PacketType)
                    {
                        case PacketType.Update:

                            if (packet.Type == typeof(PmdListenableValue<>) && packet.Field != null)
                            {
                                SubmitTask(() => UpdateListenableValue(packet.Id, packet.Field, packet.Data![0]), ProcessingOrder.Update);
                            }
                            else if (packet.Type == typeof(PmdListenableComponent<>))
                            {
                                SubmitTask(() => UpdateListenableComponent(packet.Id, packet.Data![0]), ProcessingOrder.Update);
                            }

                            break;

                        case PacketType.Add:

                            if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                            {
                                SubmitTask(() => Register(packet.Id, packet.Type, packet.Unique, packet.Data!), ProcessingOrder.Update);
                            }

                            break;

                        case PacketType.Remove:

                            if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                            {
                                SubmitTask(() => Base.DeregisterComponent(packet.Type, packet.Id, packet.Unique), ProcessingOrder.Update);
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception ex)
                {
                    //Framework.Logger.LogError("Error whilst trying to decode packet  );
                }

            }
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void SendLoop(TConn connection)
        {
            while (Running)
            {
                var packet = _globalQueue.Take(_shutdown.Token);
                if (packet.Data == null) continue;

                var packetBytes = PmdPacket.Encode(packet.Type, packet.Data, packet.Id, packet.PacketType, packet.Unique, packet.Field);
                SendBytes(connection, packetBytes);
            }
        }

        public override void Cleanup()
        {
            Running = false;
            Base.ComponentRegistered -= RegisterComponent;
            _shutdown.Cancel();
        }

    }
}
