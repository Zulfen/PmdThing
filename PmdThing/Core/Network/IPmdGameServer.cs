using PmdThing.Components.Networking;
using PmdComponentId = System.Int32;

namespace PmdThing.Core.Network
{
    // TODO: send and disconnect connections methods here?
    public interface IPmdGameServer
    {
        void RegisterHandler(QueryType type, QueryHandler handler);

        // TODO: does this need to be here
        IEnumerable<KeyValuePair<PmdComponentId, PmdComponent>> GetSyncComponents();

        // ReSharper disable once UseSymbolAlias
        int GetSyncContainerCount();
        void Cleanup();
    }
}
