using PmdThing.Core;
using PmdThing.Core.Network;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class ContainersQuery(PmdComponentConfig config, IPmdGameServer server) : QueryHandler(config)
    {
        
        public override dynamic? Handle(dynamic? requestData, IPmdServerConnection connection)
        {
            return server.GetSyncContainerCount();
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

    }
}
