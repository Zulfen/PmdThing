using PmdThing.Components.Networking;
using PmdComponentId = System.Int32;
using PmdContainerId = System.Int32;    

namespace PmdThing.Core.Network
{
    public interface IPmdGameServer
    {
        void RegisterHandler(QueryType type, QueryHandler handler);

        IEnumerable<KeyValuePair<PmdComponentId, PmdComponent>> GetSyncComponents();

        // ReSharper disable once UseSymbolAlias
        int GetSyncContainerCount();
    }
}
