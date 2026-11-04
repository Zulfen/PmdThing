using System.Net;
using PmdThing.Core;

namespace PmdThing.Components.Networking
{
    public abstract class ServerConnection(PmdComponentConfig config) : PmdComponent(config)
    {
        public abstract EndPoint? GetEndpoint();

    }
}
