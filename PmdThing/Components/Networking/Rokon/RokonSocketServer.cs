using Microsoft.Extensions.Logging;
using PmdThing.Core;
using System.Net;
using System.Net.Sockets;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonSocketServer : GameServer<IPAddress, RokonServerConnection>
    {

        private readonly Socket _serverSocket;
        private readonly int _port;

        public RokonSocketServer(PmdComponentConfig config, int port) : base(config)
        {
            _port = port;
            var ipHost = Dns.GetHostEntry("localhost");
            var ipAddr = ipHost.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddr, _port);

            _serverSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(localEndPoint);
            _serverSocket.Listen();

            var acceptThread = new Thread(AcceptConnLoop)
            {
                Name = "ServerListen"
            };
            acceptThread.Start();

            Framework.Logger.LogInformation("Listening on [{ip}:{port}]", ipAddr.MapToIPv4(), port);
        }

        private void AcceptConnLoop()
        {
            while (Running)
            {
                try
                {
                    var socket = _serverSocket.Accept();

                    Register<RokonServerConnection>(false, out var connection, socket);
                    OnClientConnected(connection);

                    var receiveThread = new Thread(() => ReceiveLoop(connection));
                    var sendThread = new Thread(() => SendLoop(connection));

                    receiveThread.Start();
                    sendThread.Start();

                }
                catch (SocketException ex)
                {
                    Framework.Logger.LogDebug("Listening loop threw error: ({exception})", ex.Message);
                }
               
            }
        }

        protected override byte[] ReceiveBytes(RokonServerConnection connection)
        {
            var buffer = new byte[2048];

            try
            {
                if (connection.Running)
                {
                    connection.Socket.Receive(buffer);
                }
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}:{port}] closed ({exception})", connection.GetEndpoint(), _port, ex.Message);
                OnClientDisconnect(connection.GetEndpoint());
            }
            
            return buffer;
        }

        protected override void SendBytes(RokonServerConnection connection, byte[] bytes)
        {
            try
            {
                if (connection.Running)
                {
                    connection.Socket.Send(bytes);
                }   
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}:{port}] closed ({exception})", connection.GetEndpoint(), _port, ex.Message);
                OnClientDisconnect(connection.GetEndpoint());
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _serverSocket.Shutdown(SocketShutdown.Both);
            _serverSocket.Close();
        }
    }
}
