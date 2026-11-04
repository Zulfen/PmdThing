using PmdThing.Core.Network.Rokon;

namespace PmdThing.Core.Network
{
    public interface IPmdServerConnection
    {
        void SendPacket(PmdPacket.PacketDto packet);
        void SignalReady();
    }
}
