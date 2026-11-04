using System.Reflection;
using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class GameServer<TAddr, TConn> : PmdNetworkHandler where TAddr : notnull where TConn : ServerConnection<TAddr>
    {

        public event Action<TConn>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        private readonly Dictionary<TAddr, TConn> _connections = [];
        private readonly Dictionary<QueryType, QueryHandler> _queryHandlers = [];

        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private readonly Queue<PmdPacket.PacketDto> _pendingQueue = [];

        private long _running = 1;
        public bool Running

        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdComponentConfig config) : base(config, typeof(PmdServerSync), PmdPacketSource.Server)
        {
            Base.ComponentRegistered += RegisterComponent;
            Base.BaseContainer.ContainerRegistered += RegisterContainer;

            Instance = this;
        }

        private void RegisterContainer(PmdContainerId id, PmdContainer container, object[]? arguments)
        {
            var containerType = container.GetType();
            if (!containerType.IsDefined(typeof(PmdServerSync))) return;

            ContainerEntries.Add(container, new ContainerEntry(id, arguments));

            if (container is PmdMultiContainer multiContainer)
            {
                multiContainer.ContainerRegistered += RegisterContainer;
            }

            PmdComponentId? parentContainerId = null;
            if (container.Parent != null && ContainerEntries.TryGetValue(container.Parent, out var parentEntry))
            {
                parentContainerId = parentEntry.Id;
            }

            var packet = new PmdPacket.PacketDto
            {
                Type = containerType,
                Data = arguments,
                PmdPacketType = PmdPacketType.AddContainer,
                Id = id,
                Unique = true,
                ParentContainer = parentContainerId,
                Source = PmdPacketSource.Server,
            };

            SendToAll(packet);
        }

        protected abstract void SendBytes(TConn connection, byte[] bytes);
        protected abstract byte[] ReceiveBytes(TConn connection);

        public void RegisterHandler(QueryType type, QueryHandler handler)
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

                while (_pendingQueue.Count > 0)
                {
                    var packet = _pendingQueue.Dequeue();
                    SendToAll(packet);
                }
            }
        }

        protected void OnClientDisconnect(TAddr address)
        {
            _connections.Remove(address, out var connection);
            connection?.Cleanup();

            Framework.Logger.LogInformation("Client disconnected: [{addr}]", address);

            SubmitTask(() => ClientDisconnected?.Invoke(address), ProcessingOrder.Update);
        }

        private void UpdateListenableValue(PmdContainerId parentId, string tag, string fieldName, object data)
        {
            var id = GetIdForTag(tag, parentId);
            var component = GetOtherComponent(id);

            var setMethod = component.GetType()
                .GetField(fieldName)?
                .GetValue(component)?
                .GetType()
                .GetMethod("Set");

            setMethod?.Invoke(component, [data]);
        }

        private void UpdateListenableComponent(PmdContainerId parentId, string tag, object data)
        {
            var id = GetIdForTag(tag, parentId);
            var component = GetOtherComponent(id);
            var value = component.GetType().GetProperty("ComponentValue");

            value?.SetValue(component, data);
        }

        public override void ListenableValueChanged(dynamic current, dynamic previous, PmdPacket.PacketDto packetDto)
        {
            var tag = GetTagForId(packetDto.Id);
            packetDto.Data = [current, tag];
            SendToAll(packetDto);
        }

        public void SendToAll(PmdPacket.PacketDto packetDto)
        {
            if (_connections.Count == 0)
            {
                _pendingQueue.Enqueue(packetDto);
            }

            foreach (var connection in _connections.Values)
            {
                connection.Queue.Add(packetDto, ShutdownTokenSrc.Token);
            }
        }

        private static void SendQueryResponse(TConn connection, PmdPacket.PacketDto reqPacket, dynamic? response)
        {
            PmdPacket.PacketDto packetDto;
            if (response == null)
            {
                packetDto = reqPacket with
                {
                    Data = [],
                };
            }
            else
            {
                packetDto = reqPacket with
                {
                    Data = [response],
                    DataType = response.GetType()
                };
            }

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

                            if (IsGenericClass(packet.Type, typeof(PmdListenableValue<>)) && packet.Attribute != null)
                            {
                                if (packet.Data == null || packet.Data.Length < 2 || packet.ParentContainer == null)
                                {
                                    Framework.Logger.LogWarning("Client [{addr}] sent an invalid update packet. Dropping connection.", connection.GetEndpoint());
                                    connection.Cleanup();
                                    continue;
                                }

                                SubmitTask(() => UpdateListenableValue(packet.ParentContainer.Value, (string)packet.Data![1], packet.Attribute, packet.Data![0]), ProcessingOrder.Update);
                            }
                            else if (IsGenericClass(packet.Type, typeof(PmdListenableComponent<>)))
                            {
                                if (packet.ParentContainer == null || packet.Attribute == null)
                                {
                                    Framework.Logger.LogWarning("Client [{addr}] sent an invalid update packet. Dropping connection.", connection.GetEndpoint());
                                    connection.Cleanup();
                                    continue;
                                }

                                SubmitTask(() => UpdateListenableComponent(packet.ParentContainer.Value, packet.Attribute!, packet.Data![0]), ProcessingOrder.Update);
                            }

                            break;

                        case PmdPacketType.AddComponent:

                            if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                            {
                                if (packet.ParentContainer.HasValue)
                                {
                                    var parentContainer = Base.GetContainer(packet.ParentContainer.Value);
                                    SubmitTask(() => parentContainer.RegisterComponent(packet.Id, packet.Type, packet.Unique, packet.Data!), ProcessingOrder.Update);
                                }
                                else
                                {
                                    SubmitTask(() => Register(packet.Id, packet.Type, packet.Unique, packet.Data!), ProcessingOrder.Update);
                                }
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
                                    SendQueryResponse(connection, packet, response);
                                }
                            }

                            break;

                        case PmdPacketType.AddContainer:
                        case PmdPacketType.DeleteContainer:
                        default:
                            throw new NotImplementedException(packet.PmdPacketType.ToString());
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

                var packetBytes = PmdPacket.Encode(packet);
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
