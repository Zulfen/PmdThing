using PmdThing.Core;
using PmdThing.Core.Network;
using PmdContainerId = System.Int32;

namespace PmdThing.Components.Networking.QueryHandlers
{
    public class MovementRequest(PmdComponentConfig config) : QueryHandler(config)
    {
        public override void Cleanup()
        {
            throw new NotImplementedException();
        }

        public override dynamic? Handle(dynamic? requestData, PmdContainerId? parentContainerId, IPmdServerConnection connection)
        {
            if (!parentContainerId.HasValue) return null;
            var transform = GetContainerComponent<TiledTransform>(parentContainerId.Value);

            if (!transform.Moving.Get())
            {
                transform.MoveInDirection(requestData);
            }
            return null;
        }
    }
}
