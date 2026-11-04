using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using System.Reflection;
using PmdComponentId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class GameServer<TAddr, TConn> : PmdComponent where TAddr : notnull where TConn : ServerConnection<TAddr>
    {

        protected class ListenableHandler(GameServer<TAddr, TConn> server, PmdPacket.PacketDto packetDto)
        {
            // ReSharper disable once UnusedMember.Global
            public void Changed(object current, object previous)
            {
                server.ListenableValueChanged(current, previous, packetDto);
            }
        }

        public event Action<TConn>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        private readonly Dictionary<TAddr, TConn> _connections = [];
        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private readonly BlockingCollection<PmdPacket.PacketDto> _globalQueue = [];
        
        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdComponentConfig config) : base(config)
        {
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

        protected void OnClientConnected(TConn connection)
        {
            var endpoint = connection.GetEndpoint();

            if (endpoint != null)
            {
                _connections.Add(endpoint, connection);
                Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

                SubmitTask(() => ClientConnected?.Invoke(connection), ProcessingOrder.Update);
            }
        }

        protected void OnClientDisconnect(TAddr address)
        {
            _connections.Remove(address, out var connection);
            connection?.Cleanup();

            Framework.Logger.LogInformation("Client disconnected: [{addr}]", address);

            SubmitTask(() => ClientDisconnected?.Invoke(address), ProcessingOrder.Update);
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
            if (!current.Equals(previous))
            {
                packetDto.Data = [current];
                _globalQueue.Add(packetDto, ShutdownTokenSrc.Token);
            }
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
                    Framework.Logger.LogError("Error ({exception}) whilst trying to decode packet from {address}", ex.Message, connection.GetEndpoint());
                    Framework.Logger.LogError("{stacktrace}", ex.StackTrace);
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
                var packet = _globalQueue.Take(ShutdownTokenSrc.Token);
                if (packet.Data == null) continue;

                var packetBytes = PmdPacket.Encode(packet.Type, packet.Data, packet.Id, packet.PacketType, packet.Unique, packet.Field);
                SendBytes(connection, packetBytes);
            }
        }

        public override void Cleanup()
        {
            Running = false;
            Base.ComponentRegistered -= RegisterComponent;
            ShutdownTokenSrc.Cancel();
        }
    }
}
