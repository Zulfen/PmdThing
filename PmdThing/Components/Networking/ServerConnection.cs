using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using System.Collections.Concurrent;

namespace PmdThing.Components.Networking
{
    public abstract class ServerConnection<TAddr>(PmdComponentConfig config) : PmdComponent(config)
    {
        public BlockingCollection<PmdPacket.PacketDto> Queue { get; } = [];
        public abstract TAddr? GetEndpoint();
    }
}
