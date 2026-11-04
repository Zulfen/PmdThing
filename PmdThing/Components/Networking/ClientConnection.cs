using DotTiled;
using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;
using System.Collections.Concurrent;
using System.Diagnostics;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking
{
    public abstract class ClientConnection<TAddr> : PmdNetworkHandler where TAddr : notnull
    {

        public event Action? Connect;
        public event Action? Disconnect;

        private readonly ConcurrentDictionary<PmdComponentId, TaskCompletionSource<dynamic?>> _queries = new();

        protected CancellationTokenSource ShutdownTokenSrc = new();

        public TaskCompletionSource<bool> ConnectionWait = new(false);
        private int _containerCount;
        private int _maxContainerCount;

        public bool Connected { get; protected set; }

        private long _running = 1;

        protected ClientConnection(PmdComponentConfig config) : base(config, typeof(PmdClientSync), PmdPacketSource.Client)
        {
            Instance = this;
            AsyncInit = true;
        }

        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        public override async Task Init()
        {
            _maxContainerCount = await SendQuery<int>(QueryType.ContainerCount);
        }

        protected abstract void SendBytes(byte[] bytes);
        protected abstract byte[] ReceiveBytes();

        protected void OnConnect()
        {
            Connected = true;
            ConnectionWait.SetResult(true);

            var endpoint = GetEndpoint();

            Framework.Logger.LogInformation("Client connected: [{addr}]", endpoint);

            SubmitTask(() => Connect?.Invoke(), ProcessingOrder.Update);
        }

        protected void OnDisconnect()
        {
            Connected = false;

            Framework.Logger.LogInformation("Disconnect from server");

            SubmitTask(() => Disconnect?.Invoke(), ProcessingOrder.Update);
        }


        private void UpdateListenableComponent(PmdContainerId parentId, string tag, object data)
        {
            var id = GetIdForTag(tag, parentId);
            if (id == null) return;

            var component = GetOtherComponent((int)id);
            var value = component.GetType().GetProperty("ComponentValue");

            value?.SetValue(component, data);
        }

        private void UpdateListenableValue(PmdContainerId parentId, string tag, string fieldName, object data)
        {
            var id = GetIdForTag(tag, parentId);
            if (id == null) return;

            var component = GetOtherComponent((int)id);

            var valueInstance = component.GetType()
                .GetField(fieldName)?
                .GetValue(component);

            var setMethod = valueInstance?
                .GetType()
                .GetMethod("Set");

            setMethod?.Invoke(valueInstance, [data]);
        }

        public override void ListenableValueChanged(dynamic current, dynamic previous, PmdPacket.PacketDto packetDto)
        {
            var tag = GetTagForId(packetDto.Id);

            packetDto.Data = [current];
            SendPacket(packetDto);

            Framework.Logger.LogDebug("Listenable value [{type}] updated ({attribute})", packetDto.Id,
                    packetDto.Attribute);
        }

        private PmdContainer GetParentContainer(PmdPacket.PacketDto packet)
        {
            if (packet.ParentContainer.HasValue && packet.ParentContainer.Value != 1)
            {
                return Base.GetContainer(packet.ParentContainer.Value);
            }

            return Base.BaseContainer;
        }

        public async Task ProcessLoop()
        {
            while (Running)
            {
                try
                {
                    var packetBytes = ReceiveBytes();
                    try
                    {
                        var packets = await PmdPacket.DecodeStreamAsync(packetBytes, ShutdownTokenSrc.Token);
                        if (packets.Count == 0)
                        {
                            Cleanup();
                            continue;
                        }

                        foreach (var packet in packets)
                        {
                            switch (packet.PmdPacketType)
                            {
                                case PmdPacketType.UpdateComponent:

                                    if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableComponent<>)))
                                    {
                                        if (packet.Data == null || packet.ParentContainer == null)
                                        {
                                            Cleanup();
                                            continue;
                                        }

                                        SubmitTask(() => UpdateListenableComponent(packet.ParentContainer.Value, packet.Tag!, packet.Data![0]), ProcessingOrder.Update);
                                    }

                                    break;

                                case PmdPacketType.AddComponent:

                                    if (packet.Type.IsSubclassOf(typeof(PmdComponent)))
                                    {
                                        if (packet.ParentContainer.HasValue)
                                        {
                                            var parentContainer = GetParentContainer(packet);
                                            if (PmdTypeUtils.IsGenericClass(packet.Type, typeof(PmdListenableComponent<>)))
                                            {
                                                SubmitTask(() => parentContainer.RegisterComponent(packet.Id, packet.Type, packet.Unique, packet.Data!), ProcessingOrder.Update);
                                            }
                                            else
                                            {
                                                SubmitTask(() => parentContainer.RegisterComponent(packet.Id, packet.Type, packet.Unique), ProcessingOrder.Update);
                                            }
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

                                    if (_queries.TryGetValue(packet.Id, out var responseTask))
                                    {
                                        responseTask.SetResult(packet.Data!.Length == 0 ? null : packet.Data![0]);
                                    }

                                    break;

                                case PmdPacketType.AddContainer:

                                    if (packet.Type.IsSubclassOf(typeof(PmdContainer)))
                                    {

                                        if (!packet.ParentContainer.HasValue)
                                        {
                                            Cleanup();
                                            continue;
                                        }

                                        var parentContainer = GetParentContainer(packet);

                                        if (parentContainer is PmdMultiContainer multiContainer)
                                        {
                                            SubmitTask(() =>
                                            {

                                                multiContainer.RegisterContainer(packet.Id, packet.Type, out _,
                                                    packet.Data!);

                                                _containerCount++;
                                                if (_containerCount == _maxContainerCount)
                                                {
                                                    SendRequest(QueryType.SendComponents);
                                                }

                                            }, ProcessingOrder.Update);

                                        }

                                    }

                                    break;

                                case PmdPacketType.UpdateValue:

                                    if (packet.ParentContainer == null || packet.Attribute == null)
                                    {
                                        Cleanup();
                                        continue;
                                    }

                                    SubmitTask(() => UpdateListenableValue(packet.ParentContainer.Value, packet.Tag!, packet.Attribute, packet.Data![0]), ProcessingOrder.Update);

                                    break;

                                case PmdPacketType.DeleteContainer:
                                default:
                                    throw new NotImplementedException();
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Framework.Logger.LogError("Error ({exception}) whilst trying to decode packet from {address}", ex.Message, GetEndpoint());
                        Framework.Logger.LogError("{stacktrace}", ex.StackTrace);

                        Cleanup();
                    }


                }
                catch (OperationCanceledException)
                {
                    break;
                }

            }
        }

        // TODO: packet queue?
        public void SendPacket(PmdPacket.PacketDto packet)
        {
            if (ConnectionWait.Task.Result)
            {
                var packetBytes = PmdPacket.Encode(packet);
                SendBytes(packetBytes);
            }
        }

        public async Task SendPacketAsync(PmdPacket.PacketDto packet)
        {
            if (await ConnectionWait.Task)
            {
                var packetBytes = PmdPacket.Encode(packet);
                SendBytes(packetBytes);
            }
        }

        public void SendRequest(QueryType type)
        {
            var packet = new PmdPacket.PacketDto
            {
                Attribute = type.ToString(),
                Type = typeof(QueryType),
                PmdPacketType = PmdPacketType.Query,
                Unique = true,
                Id = Random.Shared.Next(),
                Source = PmdPacketSource.Client
            };
            
            SendPacket(packet);
            Framework.Logger.LogDebug("Sent request [{type}]", type);
        }

        public async Task<T?> SendQuery<T>(QueryType type, object? data = null) where T : notnull
        {

            var packet = new PmdPacket.PacketDto
            {
                Attribute = type.ToString(),
                Data = data != null ? [data] : null,
                DataType = data?.GetType(),
                Type = typeof(QueryType),
                PmdPacketType = PmdPacketType.Query,
                Unique = true,
                Id = Random.Shared.Next(),
                Source = PmdPacketSource.Client
            };

            var responseTask = new TaskCompletionSource<dynamic?>();
            _queries.TryAdd(packet.Id, responseTask);
            await SendPacketAsync(packet);

            Framework.Logger.LogDebug("Sent query [{type}] with data [{data}]", type, data);

            var response = await responseTask.Task;
            if (response is T validData)
            {
                _queries.TryRemove(packet.Id, out _);
                return validData;
            }

            throw new InvalidCastException($"Expected type {typeof(T)} but got {response?.GetType()}");

        }

        public abstract TAddr GetEndpoint();

        public abstract bool IsNetworkConnected();

        public override void Cleanup()
        {
            Running = false;
            if (!ConnectionWait.Task.IsCompleted)
            {
                ConnectionWait.SetResult(false);
            }
            ShutdownTokenSrc.Cancel();
            OnDisconnect();
        }
    }
}
