using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using System.Net;
using System.Net.Sockets;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonClientConnection : ClientConnection<IPAddress>
    {

        private readonly IPAddress _ip;
        private readonly Socket _socket;

        public RokonClientConnection(PmdComponentConfig config, string host, int port) : base(config)
        {
            var ipHostInfo = Dns.GetHostEntry(host);
            _ip = ipHostInfo.AddressList[0];

            _socket = new Socket(_ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_ip, port);

            var sendThread = new Thread(SendLoop);
            var receiveThread = new Thread(ReceiveLoop);

            sendThread.Start();
            receiveThread.Start();

            OnConnect();
        }

        public override IPAddress GetEndpoint()
        {
            return _ip;
        }

        public override void Cleanup()
        {
            _socket.Close();
            base.Cleanup();
        }

        protected override byte[] ReceiveBytes()
        {
            var buffer = new byte[2048];

            try
            {
                _socket.Receive(buffer);
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", GetEndpoint(), ex.Message);
                OnDisconnect();
            }

            return buffer;
        }

        protected override void SendBytes(byte[] bytes)
        {
            try
            {
                _socket.Send(bytes);
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", GetEndpoint(), ex.Message);
                OnDisconnect();
            }
        }
    }
}
