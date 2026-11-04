using Microsoft.Extensions.Logging;
using PmdThing.Core;
using System.Net;
using System.Net.Sockets;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonSocketServer : GameServer<IPAddress, RokonServerConnection>
    {

        private readonly Socket _serverSocket;

        public RokonSocketServer(PmdComponentConfig config, int port) : base(config)
        {
            var ipHost = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddr = ipHost.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddr, port);

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
                connection.Socket.Receive(buffer);
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", connection.GetEndpoint(), ex.Message);
                OnClientDisconnect(connection.GetEndpoint());
            }
            
            return buffer;
        }

        protected override void SendBytes(RokonServerConnection connection, byte[] bytes)
        {
            try
            {
                connection.Socket.Send(bytes);
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", connection.GetEndpoint(), ex.Message);
                OnClientDisconnect(connection.GetEndpoint());
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _serverSocket.Close();
        }
    }
}
