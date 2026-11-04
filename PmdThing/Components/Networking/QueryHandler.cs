using DotTiled;
using PmdThing.Core;
using PmdThing.Core.Network;

namespace PmdThing.Components.Networking
{
    public abstract class QueryHandler(PmdComponentConfig config) : PmdComponent(config)
    {
        public abstract dynamic? Handle(dynamic? requestData, int? parentContainerId, IPmdServerConnection connection);
    }
}
