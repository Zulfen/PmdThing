using System.Net;
using System.Net.Sockets;
using PmdThing.Core;
using PmdThing.Core.Network;
using PmdThing.Core.Network.Rokon;
using PmdThing.Core.Util;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonServerConnection : ServerConnection<IPEndPoint>
    {
        private readonly IPEndPoint _endPoint;
        private bool _socketClosed;

        public Socket Socket { get; }
        public readonly Lock SocketLock = new();

        private readonly RokonSocketServer _server;

        public RokonServerConnection(PmdComponentConfig config, Socket socket) : base(config)
        {
            _server = GetBaseContainer<RokonSocketServer>();
            Socket = socket;

            _endPoint = socket.RemoteEndPoint is not IPEndPoint endpoint
                ? throw new InvalidOperationException("Socket is not connected")
                : endpoint;
        }

        public override IPEndPoint GetEndpoint()
        {
            return _endPoint;
        }

        public override void SendPacket(PmdPacket.PacketDto packet)
        {
            _server.SendPacket(packet, this);
        }

        public override void SignalReady()
        {
            base.SignalReady();
            SubmitTask(() => _server.OnClientJoined(this), ProcessingOrder.Update);
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