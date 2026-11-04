using System.Net;
using System.Net.Sockets;
using PmdThing.Core;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonServerConnection : ServerConnection<IPAddress>
    {
        private readonly IPAddress _ip;
        private bool _socketClosed = false;

        public RokonServerConnection(PmdComponentConfig config, Socket socket) : base(config)
        {
            Socket = socket;

            _ip = socket.RemoteEndPoint is not IPEndPoint endpoint ? throw new InvalidOperationException("Socket is not connected") : endpoint.Address;
        }

        public Socket Socket { get; }

        public override void Cleanup()
        {
            if (!_socketClosed)
            {
                base.Cleanup();
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();

                _socketClosed = true;
            }
        }

        public override IPAddress GetEndpoint()
        {
            return _ip;
        }
    }
}
