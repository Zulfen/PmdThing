using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using System.Collections.Concurrent;

namespace PmdThing.Components.Networking
{
    public abstract class ServerConnection<TAddr>(PmdComponentConfig config) : PmdComponent(config)
    {
        public BlockingCollection<PmdPacket.PacketDto> SendQueue { get; } = [];
        public BlockingCollection<PmdPacket.PacketDto> ReceiveQueue { get; } = [];

        public readonly CancellationTokenSource ShutdownTokenSrc = new();

        public abstract TAddr? GetEndpoint();

        private long _running = 1;
        public bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        public override void Cleanup()
        {
            Running = false;
            ShutdownTokenSrc.Cancel();
        }
    }
}
