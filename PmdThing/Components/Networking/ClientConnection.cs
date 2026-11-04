using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using PmdComponentId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class ClientConnection<TAddr> : PmdNetworkComponent where TAddr : notnull
    {

        public event Action? Connected;
        public event Action? Disconnected;
        private readonly BlockingCollection<PmdPacket.PacketDto> _sendQueue = [];

        private readonly Dictionary<QueryType, QueryHandler> _queryHandlers = [];

        private readonly Dictionary<PmdComponentId, TaskCompletionSource<dynamic>> _queries = [];
        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected ClientConnection(PmdComponentConfig config) : base(config, typeof(PmdClientSync), PmdPacketSource.Client)
        {
            Base.ComponentRegistered += RegisterComponent;
        }

        protected abstract void SendBytes(byte[] bytes);
        protected abstract byte[] ReceiveBytes();

        protected void RegisterHandler(QueryType type, QueryHandler handler)
        {
            _queryHandlers.Add(type, handler);
        }

        protected void OnConnect()
        {
            var endpoint = GetEndpoint();

            Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

            SubmitTask(() => Connected?.Invoke(), ProcessingOrder.Update);
        }

        protected void OnDisconnect()
        {
            Framework.Logger.LogInformation("Disconnected from server");

            SubmitTask(() => Disconnected?.Invoke(), ProcessingOrder.Update);
            Cleanup();
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
                _sendQueue.Add(packetDto, ShutdownTokenSrc.Token);
            }
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void ReceiveLoop()
        {
            while (Running)
            {
                var packetBytes = ReceiveBytes();
                try
                {
                    var packet = PmdPacket.Decode(packetBytes);
                    if (!PmdPacket.ValidPacket(packet))
                    {
                        Framework.Logger.LogWarning("Server sent an invalid packet. Dropping connection.");
                        Cleanup();
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

                            if (_queries.TryGetValue(packet.Id, out var responseTask))
                            {
                                responseTask.SetResult(packet.Data![0]);
                            }

                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception ex)
                {
                    Framework.Logger.LogError("Error ({exception}) whilst trying to decode packet from {address}", ex.Message, GetEndpoint());
                    Framework.Logger.LogError("{stacktrace}", ex.StackTrace);
                }

            }
        }

        /// <summary>
        /// Note: start this in another thread
        /// </summary>
        public void SendLoop()
        {
            while (Running)
            {
                var packet = _sendQueue.Take(ShutdownTokenSrc.Token);
                if (packet.Data == null) continue;

                var packetBytes = PmdPacket.Encode(packet.Type, packet.Data, packet.Id, packet.PmdPacketType, packet.Unique, packet.Attribute, packet.DataType, PmdPacketSource.Client);
                SendBytes(packetBytes);
            }
        }

        public async Task<T> SendQuery<T>(QueryType type, object data) where T : notnull
        {
            if (_queryHandlers.TryGetValue(type, out var handler))
            {
                if (data.GetType() != handler.GetDataType())
                {
                    throw new ArgumentException(
                        $"Handler for {type} only accepts instances of {handler.GetDataType()}");
                }

                var packetDto = new PmdPacket.PacketDto
                {
                    Attribute = type.ToString(),
                    Data = [data],
                    DataType = data.GetType(),
                    PmdPacketType = PmdPacketType.Query,
                    Type = handler.GetDataType(),
                    Unique = true,
                    Id = Random.Shared.Next(),
                    Source = PmdPacketSource.Client
                };

                var responseTask = new TaskCompletionSource<object>();

                _queries.Add(packetDto.Id, responseTask);
                _sendQueue.Add(packetDto, ShutdownTokenSrc.Token);

                if (await responseTask.Task is T response)
                {
                    _queries.Remove(packetDto.Id);
                    return response;
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            throw new ArgumentException("No handler found for the specified query type.");
        }

        public abstract TAddr GetEndpoint();

        public override void Cleanup()
        {
            Running = false;
            Base.ComponentRegistered -= RegisterComponent;
            ShutdownTokenSrc.Cancel();
        }
    }
}
