using Microsoft.Extensions.Logging;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Net;
using System.Reflection;
using PmdContainerId = System.Int32;
using PmdComponentId = System.Int32;

namespace PmdThing.Components.Networking
{
    // TODO: make this a container
    public abstract class GameServer<TAddr, TConn> : PmdNetworkHandler where TAddr : notnull where TConn : ServerConnection<TAddr>
    {

        public event Action<TConn>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        private readonly Dictionary<TAddr, TConn> _connections = [];
        private readonly Dictionary<QueryType, QueryHandler> _queryHandlers = [];

        private readonly Dictionary<PmdContainerId, PmdPacket.PacketDto> _containerTracker = [];
        private readonly Dictionary<TConn, PmdComponentId> _connectionIds = [];

        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private long _running = 1;
        public bool Running

        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdComponentConfig config) : base(config, typeof(PmdServerSync), PmdPacketSource.Server)
        {
            Base.BaseContainer.ContainerRegistered += RegisterContainer;
            ClientConnected += GameServer_ClientConnected;
            Instance = this;
        }

        private void GameServer_ClientConnected(TConn connection)
        {
            foreach (var id in Base.BaseContainer.GetAllContainers())
            {
                if (_containerTracker.TryGetValue(id, out var packet))
                {
                    connection.SendQueue.TryAdd(packet);
                    Framework.Logger.LogDebug("Sending container update ({type}) ({action}) to client: [{addr}]", packet.Type, packet.PmdPacketType, connection.GetEndpoint());
                }
            }
        }

        private void RegisterContainer(PmdContainerId id, PmdContainer container, object[]? arguments)
        {
            var containerType = container.GetType();
            if (!containerType.IsDefined(typeof(PmdServerSync))) return;

            if (container is PmdMultiContainer multiContainer)
            {
                multiContainer.ContainerRegistered += RegisterContainer;
            }

            var packet = new PmdPacket.PacketDto
            {
                Type = containerType,
                Data = arguments,
                PmdPacketType = PmdPacketType.AddContainer,
                Id = id,
                Unique = true,
                ParentContainer = container.Parent?.Id ?? Base.BaseContainer.Id,
                Source = PmdPacketSource.Server,
            };

            _containerTracker.Add(id, packet);

            SendToAll(packet);
        }

        protected abstract void SendBytes(TConn connection, byte[] bytes);
        protected abstract byte[] ReceiveBytes(TConn connection);

        public void RegisterHandler(QueryType type, QueryHandler handler)
        {
            _queryHandlers.Add(type, handler);
        }

        protected void OnClientConnected(TConn connection, PmdComponentId id)
        {
            var endpoint = connection.GetEndpoint();

            if (endpoint != null)
            {
                _connections.Add(endpoint, connection);
                _connectionIds.Add(connection, id);

                Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

                SubmitTask(() => ClientConnected?.Invoke(connection), ProcessingOrder.Update);

                SubmitTask(() => Base.RegisterContainer<TestPokemon>(out _, PmdConstants.Pokemon.Eevee, false), ProcessingOrder.Update);
            }
        }

        protected void OnClientDisconnect(TConn connection, TAddr endpoint)
        {
            _connections.Remove(endpoint);
            DeregisterParent<TConn>(_connectionIds[connection], true);

            Framework.Logger.LogInformation("Client disconnected: [{addr}]", endpoint);
            SubmitTask(() => ClientDisconnected?.Invoke(endpoint), ProcessingOrder.Update);
        }

        private void UpdateListenableValue(PmdContainerId parentId, string tag, string fieldName, object data)
        {
            var id = GetIdForTag(tag, parentId);
            if (id == null) return;

            var component = GetOtherComponent((int)id);

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
            if (id == null) return;

            var component = GetOtherComponent((int)id);
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
            foreach (var connection in _connections.Values)
            {
                connection.SendQueue.TryAdd(packetDto);
            }
        }

        private void SendQueryResponse(TConn connection, PmdPacket.PacketDto reqPacket, dynamic? response)
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

            Framework.Logger.LogDebug("Sending query response ({type}) to [{addr}]", packetDto.DataType, connection.GetEndpoint());

            connection.SendQueue.TryAdd(packetDto);
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void ReceiveLoop(TConn connection)
        {
            while (Running && connection.Running)
            {
                var packetBytes = ReceiveBytes(connection);
                try
                {
                    var packet = PmdPacket.Decode(packetBytes);
                    if (packet == null)
                    {
                        continue;
                    }   
                    connection.ReceiveQueue.TryAdd(packet);

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
        public void ProcessLoop(TConn connection)
        {
            while (Running && connection.Running)
            {

                try
                {

                    var packet = connection.ReceiveQueue.Take(connection.ShutdownTokenSrc.Token);

                    switch (packet.PmdPacketType)
                    {
                        case PmdPacketType.UpdateComponent:

                            if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableValue<>)) && packet.Attribute != null)
                            {
                                if (packet.Data == null || packet.Data.Length < 2 || packet.ParentContainer == null)
                                {
                                    connection.Cleanup();
                                    continue;
                                }

                                SubmitTask(() => UpdateListenableValue(packet.ParentContainer.Value, (string)packet.Data![1], packet.Attribute, packet.Data![0]), ProcessingOrder.Update);
                            }
                            else if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableComponent<>)))
                            {
                                if (packet.Attribute == null || packet.ParentContainer == null)          
                                {
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

                            if (Enum.TryParse(packet.Attribute, out QueryType reqType))
                            {
                                if (_queryHandlers.TryGetValue(reqType, out var handler))
                                {
                                    var response = handler.Handle(packet.Data![0]);
                                    SendQueryResponse(connection, packet, response);
                                }
                                else
                                {
                                    throw new InvalidDataException();
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("Invalid query type");
                            }

                            break;

                        case PmdPacketType.AddContainer:
                        case PmdPacketType.DeleteContainer:
                        default:
                            throw new NotImplementedException(packet.PmdPacketType.ToString());
                    }

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Framework.Logger.LogError("Error ({exception}) whilst trying to process packet from {address}", ex.Message, connection.GetEndpoint());
                    Framework.Logger.LogError("{stacktrace}", ex.StackTrace);
                    connection.Cleanup();
                }

            }
        }

        public void Disconnect(TConn connection)
        {
            connection.Running = false;

            var endpoint = connection.GetEndpoint();
            if (endpoint != null)
            {
                OnClientDisconnect(connection, endpoint);
            }
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void SendLoop(TConn connection)
        {
            while (Running && connection.Running)
            {
                try
                {
                    var packet = connection.SendQueue.Take(connection.ShutdownTokenSrc.Token);
                    if (packet == null) throw new InvalidOperationException();

                    var packetBytes = PmdPacket.Encode(packet);
                    SendBytes(connection, packetBytes);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public override void Cleanup()
        {
            Running = false;
            ShutdownTokenSrc.Cancel();

            foreach (var connection in _connections.Values)
            {
                connection.Cleanup();
            }
        }
    }
}
