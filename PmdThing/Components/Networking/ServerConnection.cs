using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;

namespace PmdThing.Components.Networking
{
    public abstract class ServerConnection<TAddr>(PmdComponentConfig config) : PmdComponent(config), IPmdServerConnection
    {
        public readonly CancellationTokenSource ShutdownTokenSrc = new();
        
        public bool Ready { get; private set; }

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

        public abstract void SendPacket(PmdPacket.PacketDto packet);

        public void SignalReady()
        {
            Ready = true;
        }
    }
}
