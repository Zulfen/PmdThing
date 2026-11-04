using Microsoft.Extensions.Logging;
using PmdThing.Core;
using PmdThing.Core.Network.Rokon;
using System.Net;
using System.Net.Sockets;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonClientConnection : ClientConnection<IPAddress>
    {
        private int _port;
        private readonly IPAddress _ip;
        private Socket? _socket;

        public RokonClientConnection(PmdComponentConfig config, string host, int port) : base(config)
        {
            AsyncInit = true;
            _port = port;

            var ipHostInfo = Dns.GetHostEntry(host);
            _ip = ipHostInfo.AddressList[0];
        }

        public override async Task Init()
        {
            while (!Connected)
            {
                try
                {
                    _socket = new Socket(_ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await _socket.ConnectAsync(_ip, _port);

                    var sendThread = new Thread(SendLoop);
                    var receiveThread = new Thread(ReceiveLoop);

                    sendThread.Start();
                    receiveThread.Start();

                    OnConnect();
                }
                catch (SocketException ex)
                {
                    Framework.Logger.LogWarning("Failed to connect to [{address}:{port}]: {exception}", GetEndpoint(), _port, ex.Message);
                }
            }
           
        }

        public override IPAddress GetEndpoint()
        {
            return _ip;
        }

        public override void Cleanup()
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
            base.Cleanup();
        }

        protected override byte[] ReceiveBytes()
        {
            var buffer = new byte[2048];

            try
            {
                _socket?.Receive(buffer);
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
                _socket?.Send(bytes);
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", GetEndpoint(), ex.Message);
                OnDisconnect();
            }
        }
    }
}
