using PmdThing.Components.Networking;

namespace PmdThing.Core.Network
{
    public interface IPmdGameClient
    {
        Task<T?> SendQuery<T>(QueryType type, PmdContainer? parent = null, object? data = null) where T : notnull;
        void SendRequest(QueryType type, PmdContainer? parent = null, object? data = null);
        void Cleanup();
    }
}
