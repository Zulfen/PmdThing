using PmdThing.Core;
using PmdThing.Core.Network;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class ContainersQuery(PmdComponentConfig config) : QueryHandler(config)
    {
        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override dynamic? Handle(dynamic? requestData, PmdContainerId? parentContainerId, IPmdServerConnection connection)
        {
            return Server?.GetSyncContainerCount();
        }
    }
}
