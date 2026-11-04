using System.Net;
using System.Net.Sockets;
using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonServerConnection : ServerConnection<IPAddress>
    {
        private readonly IPAddress _ip;
        private bool _socketClosed;

        public Socket Socket { get; }
        public readonly Lock SocketLock = new();

        private readonly RokonSocketServer _server;

        public RokonServerConnection(PmdComponentConfig config, Socket socket) : base(config)
        {
            _server = GetSharedComponent<RokonSocketServer>();
            Socket = socket;

            _ip = socket.RemoteEndPoint is not IPEndPoint endpoint
                ? throw new InvalidOperationException("Socket is not connected")
                : endpoint.Address;
        }

        public override IPAddress GetEndpoint()
        {
            return _ip.MapToIPv4();
        }

        public override void SendPacket(PmdPacket.PacketDto packet)
        {
            _server.SendPacket(packet, this);
        }


        public override void Cleanup()
        {
            lock (SocketLock)
            {
                if (!_socketClosed)
                {
                    base.Cleanup();
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Close(1000);

                    _socketClosed = true;

                    _server.Disconnect(this);
                }
            }
        }

     
    }
}