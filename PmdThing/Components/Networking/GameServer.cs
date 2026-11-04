using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using PmdComponentId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class GameServer<TAddr, TConn> : PmdNetworkComponent where TAddr : notnull where TConn : ServerConnection<TAddr>
    {

        public event Action<TConn>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        private readonly Dictionary<TAddr, TConn> _connections = [];
        private readonly Dictionary<QueryType, QueryHandler> _queryHandlers = [];
        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdComponentConfig config) : base(config, typeof(PmdServerSync), PmdPacketSource.Server)
        {
            Base.ComponentRegistered += RegisterComponent;
        }

        protected abstract void SendBytes(TConn connection, byte[] bytes);
        protected abstract byte[] ReceiveBytes(TConn connection);

        protected void RegisterHandler(QueryType type, QueryHandler handler)
        {
            _queryHandlers.Add(type, handler);
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

        public override void ListenableValueChanged(dynamic current, dynamic previous, PmdPacket.PacketDto packetDto)
        {
            if (!current.Equals(previous))
            {
                packetDto.Data = [current];
                SendToAll(packetDto);
            }
        }

        public void SendToAll(PmdPacket.PacketDto packetDto)
        {
            foreach (var connection in _connections.Values)
            {
                connection.Queue.Add(packetDto, ShutdownTokenSrc.Token);
            }
        }

        private void SendQueryResponse(TConn connection, PmdPacket.PacketDto reqPacket, QueryType queryType, dynamic response)
        {
            var packetDto = reqPacket with
            {
                Data = response
            };

            connection.Queue.Add(packetDto);
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
                    if (!PmdPacket.ValidPacket(packet))
                    {
                        Framework.Logger.LogWarning("Client [{addr}] sent an invalid packet. Dropping connection.", connection.GetEndpoint());
                        connection.Cleanup();
                        continue;
                    }

                    switch (packet.PmdPacketType)
                    {
                        case PmdPacketType.UpdateComponent:

                            if (packet.Type == typeof(PmdListenableValue<>) && packet.Attribute != null)
                            {
                                SubmitTask(() => UpdateListenableValue(packet.Id, packet.Attribute, packet.Data![0]), ProcessingOrder.Update);
                            }
                            else if (packet.Type == typeof(PmdListenableComponent<>))
                            {
                                SubmitTask(() => UpdateListenableComponent(packet.Id, packet.Data![0]), ProcessingOrder.Update);
                            }

                            break;

                        case PmdPacketType.AddComponent:

                            if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                            {
                                SubmitTask(() => Register(packet.Id, packet.Type, packet.Unique, packet.Data!), ProcessingOrder.Update);
                            }
                            else if (packet.Type.IsSubclassOf(typeof(PmdContainer)))
                            {
                                SubmitTask(() => Base.RegisterContainer(packet.Id, packet.Type, packet.Data), ProcessingOrder.Update);
                            }

                            break;

                        case PmdPacketType.RemoveComponent:

                            if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                            {
                                SubmitTask(() => Base.DeregisterComponent(packet.Type, packet.Id, packet.Unique), ProcessingOrder.Update);
                            }

                            break;

                        case PmdPacketType.Query:

                            if (packet.Attribute == null) break;
                            if (Enum.TryParse(packet.Attribute, out QueryType reqType))
                            {
                                if (_queryHandlers.TryGetValue(reqType, out var handler))
                                {
                                    var response = handler.Handle(packet.Data![0]);
                                    SendQueryResponse(connection, packet, reqType, response);
                                }
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

                    connection.Cleanup();
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
                var packet = connection.Queue.Take(ShutdownTokenSrc.Token);
                if (packet.Data == null) continue;

                var packetBytes = PmdPacket.Encode(packet.Type, packet.Data, packet.Id, packet.PmdPacketType, packet.Unique, packet.Attribute, packet.DataType, packet.Source);
                SendBytes(connection, packetBytes);
            }
        }

        public override void Cleanup()
        {
            Running = false;
            Base.ComponentRegistered -= RegisterComponent;
            ShutdownTokenSrc.Cancel();

            foreach (var connection in _connections.Values)
            {
                connection.Cleanup();
            }
        }
    }
}
