using Microsoft.Extensions.Logging;
using PmdThing.Containers;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using System.Reflection;
using PmdThing.Core.Network;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking
{
    // TODO: make this a container
    public abstract class GameServer<TAddr, TConn> : PmdNetworkHandler, IPmdGameServer where TAddr : notnull where TConn : ServerConnection<TAddr>
    {

        public event Action<TConn>? ClientConnected;
        public event Action<TAddr>? ClientDisconnected;

        private readonly Dictionary<TAddr, TConn> _connections = [];
        private readonly Dictionary<QueryType, QueryHandler> _queryHandlers = [];

        private readonly ConcurrentDictionary<PmdComponentId, PmdComponent> _syncComponents = [];
        private readonly ConcurrentDictionary<PmdContainerId, PmdPacket.PacketDto> _containerTracker = [];

        private readonly Dictionary<TConn, PmdComponentId> _connectionIds = [];

        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private long _running = 1;
        public bool Running

        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected GameServer(PmdContainerConfig config) : base(config, typeof(PmdServerSync), PmdPacketSource.Server)
        {
            Base.BaseContainer.ContainerRegistered += RegisterContainer;
            Instance = this;
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
                Parent = container.Parent?.Id ?? Base.BaseContainer.Id,
                Source = PmdPacketSource.Server,
            };

            _containerTracker.TryAdd(id, packet);

            SendToAll(packet);
        }

        public override void RegisterSyncedComponent(PmdComponentId id, PmdComponent component, bool unique, PmdContainer parent, string tag)
        {
            base.RegisterSyncedComponent(id, component, unique, parent, tag);
            _syncComponents.TryAdd(id, component);
        }

        protected abstract void SendBytes(TConn connection, byte[] bytes);
        protected abstract byte[] ReceiveBytes(TConn connection);

        public void RegisterHandler(QueryType type, QueryHandler handler)
        {
            _queryHandlers.TryAdd(type, handler);
        }

        protected void OnClientConnected(TConn connection, PmdComponentId id)
        {
            var endpoint = connection.GetEndpoint();

            if (endpoint != null)
            {
                _connections.Add(endpoint, connection);
                _connectionIds.Add(connection, id);

                Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

                Base.SubmitTask(() => ClientConnected?.Invoke(connection));


                foreach (var containerId in Base.BaseContainer.GetAllContainers())
                {
                    if (_containerTracker.TryGetValue(containerId, out var packet))
                    {
                        SendPacket(packet, connection);
                        Framework.Logger.LogDebug("Sending container update ({type}) ({action}) to client: [{addr}]", packet.Type, packet.PmdPacketType, connection.GetEndpoint());
                    }
                }

            }
        }

        protected void OnClientDisconnect(TConn connection, TAddr endpoint)
        {
            _connections.Remove(endpoint);
            Deregister<TConn>(_connectionIds[connection], true);

            Framework.Logger.LogInformation("Client disconnected: [{addr}]", endpoint);
            Base.SubmitTask(() => ClientDisconnected?.Invoke(endpoint));
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
            packetDto.Data = [current];
            foreach (var connection in _connections.Values)
            {
                SendPacket(packetDto, connection);
            }
        }

        public void SendToAll(PmdPacket.PacketDto packetDto)
        {
            foreach (var connection in _connections.Values)
            {
                SendPacket(packetDto, connection);
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

            //Framework.Logger.LogDebug("Sending query response ({type}) to [{addr}]", packetDto.DataType, connection.GetEndpoint());

            SendPacket(packetDto, connection);
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public async Task ProcessLoop(TConn connection)
        {
            while (Running && connection.Running)
            {

                try
                {

                    var packetBytes = ReceiveBytes(connection);
                    try
                    {
                        var packets = await PmdPacket.DecodeStreamAsync(packetBytes, connection.ShutdownTokenSrc.Token);

                        foreach (var packet in packets)
                        {
                            switch (packet.PmdPacketType)
                            {

                                case PmdPacketType.Query:

                                    if (Enum.TryParse(packet.Attribute, out QueryType reqType))
                                    {
                                        if (_queryHandlers.TryGetValue(reqType, out var handler))
                                        {
                                            var response = handler.Handle(packet.Data?[0], packet.Parent, connection);
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

                                case PmdPacketType.AddComponent:
                                case PmdPacketType.RemoveComponent:
                                case PmdPacketType.UpdateComponent:
                                case PmdPacketType.UpdateValue: 
                                case PmdPacketType.AddContainer:
                                case PmdPacketType.DeleteContainer:
                                default:
                                    throw new NotImplementedException(packet.PmdPacketType.ToString());
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Framework.Logger.LogError("Error ({exception}) whilst trying to decode packet from {address}", ex.Message, connection.GetEndpoint());
                        Framework.Logger.LogError("{stacktrace}", ex.StackTrace);

                        connection.Cleanup();
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

        public void SendPacket(PmdPacket.PacketDto packet, TConn connection)
        {
            var packetBytes = PmdPacket.Encode(packet);
            SendBytes(connection, packetBytes);
        }

        public IEnumerable<KeyValuePair<PmdComponentId, PmdComponent>> GetSyncComponents()
        {
            return _syncComponents;
        }

        public int GetSyncContainerCount()
        {
            return _containerTracker.Count;
        }

        public virtual void Cleanup()
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
