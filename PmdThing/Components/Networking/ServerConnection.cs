using System.Net;
using PmdThing.Core;

namespace PmdThing.Components.Networking
{
    public abstract class ServerConnection<TAddr>(PmdComponentConfig config) : PmdComponent(config)
    {
        public abstract TAddr? GetEndpoint();
    }
}
