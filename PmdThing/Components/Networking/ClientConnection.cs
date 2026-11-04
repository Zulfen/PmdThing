using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class ClientConnection<TAddr> : PmdNetworkHandler where TAddr : notnull
    {

        public event Action? Connect;
        public event Action? Disconnect;
        private readonly BlockingCollection<PmdPacket.PacketDto> _sendQueue = [];

        private readonly Dictionary<PmdComponentId, TaskCompletionSource<dynamic?>> _queries = [];

        protected readonly CancellationTokenSource ShutdownTokenSrc = new();

        public bool Connected { get; protected set; }

        private long _running = 1;

        protected ClientConnection(PmdComponentConfig config) : base(config, typeof(PmdClientSync), PmdPacketSource.Client)
        {
            Instance = this;
        }

        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        protected abstract void SendBytes(byte[] bytes);
        protected abstract byte[] ReceiveBytes();

        protected void OnConnect()
        {
            Connected = true;
            var endpoint = GetEndpoint();

            Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

            SubmitTask(() => Connect?.Invoke(), ProcessingOrder.Update);
        }

        protected void OnDisconnect()
        {
            Framework.Logger.LogInformation("Disconnect from server");

            SubmitTask(() => Disconnect?.Invoke(), ProcessingOrder.Update);
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
            _sendQueue.Add(packetDto, ShutdownTokenSrc.Token);

            Framework.Logger.LogDebug("Listenable value [{type}] updated ({attribute})", packetDto.Id,
                    packetDto.Attribute);
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
                    if (packet == null || (!PmdPacket.ValidPacket(packet) && packet.PmdPacketType != PmdPacketType.Query))
                    {
                        Framework.Logger.LogWarning("Server sent an invalid packet. Dropping connection.");
                        Cleanup();
                        continue;
                    }

                    switch (packet.PmdPacketType)
                    {
                        case PmdPacketType.UpdateComponent:

                            if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableValue<>)) && packet.Attribute != null)
                            {
                                if (packet.Data == null || packet.Data.Length < 2 || packet.ParentContainer == null)
                                {
                                    Cleanup();
                                    continue;
                                }

                                SubmitTask(() => UpdateListenableValue(packet.ParentContainer.Value, (string)packet.Data![1], packet.Attribute, packet.Data![0]), ProcessingOrder.Update);
                            }
                            else if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableComponent<>)))
                            {
                                if (packet.ParentContainer == null || packet.Attribute == null)
                                {
                                    Cleanup();
                                    continue;
                                }

                                SubmitTask(() => UpdateListenableComponent(packet.ParentContainer.Value, packet.Attribute!, packet.Data![0]), ProcessingOrder.Update);
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
                                responseTask.SetResult(packet.Data!.Length == 0 ? null : packet.Data![0]);
                            }

                            break;

                        case PmdPacketType.AddContainer:

                            if (packet.Type.IsSubclassOf(typeof(PmdContainer)))
                            {
                                if (packet.ParentContainer.HasValue && packet.ParentContainer.Value != Base.BaseContainer.Id)
                                {
                                    var parentContainer = Base.GetContainer(packet.ParentContainer.Value);
                                    if (parentContainer is PmdMultiContainer multiContainer)
                                    {
                                        SubmitTask(() => multiContainer.RegisterContainer(packet.Id, packet.Type, out _, packet.Data!), ProcessingOrder.Update);
                                    }
                                }
                                else
                                {
                                    SubmitTask(() => Base.RegisterContainer(packet.Id, packet.Type, packet.Data!), ProcessingOrder.Update);
                                }
                            }

                            break;

                        case PmdPacketType.DeleteContainer:
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception ex)
                {
                    Framework.Logger.LogError("Error ({exception}) whilst trying to decode packet from {address}", ex.Message, GetEndpoint());
                    Framework.Logger.LogError("{stacktrace}", ex.StackTrace);

                    Cleanup();
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
                try
                {

                    var packet = _sendQueue.Take(ShutdownTokenSrc.Token);
                    if (packet.Data == null) continue;

                    var packetBytes = PmdPacket.Encode(packet);
                    SendBytes(packetBytes);

                } 
                catch (OperationCanceledException)
                {
                    break;
                }
                
            }
        }

        public async Task<T?> SendQuery<T>(QueryType type, object data) where T : notnull
        {

            var packetDto = new PmdPacket.PacketDto
            {
                Attribute = type.ToString(),
                Data = [data],
                DataType = data.GetType(),
                Type = data.GetType(),
                PmdPacketType = PmdPacketType.Query,
                Unique = true,
                Id = Random.Shared.Next(),
                Source = PmdPacketSource.Client
            };

            var responseTask = new TaskCompletionSource<dynamic?>();

            _queries.Add(packetDto.Id, responseTask);
            try
            {
                _sendQueue.Add(packetDto, ShutdownTokenSrc.Token);
            }
            catch (OperationCanceledException)
            {
                return default;
            }

            Framework.Logger.LogDebug("Sent query [{type}] with data [{data}]", type, data);

            if (await responseTask.Task is T response)
            {
                _queries.Remove(packetDto.Id);
                return response;
            }

            return default;

        }

        public abstract TAddr GetEndpoint();

        public override void Cleanup()
        {
            Running = false;
            ShutdownTokenSrc.Cancel();
        }
    }
}
