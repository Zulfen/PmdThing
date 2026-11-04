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

        public RokonSocketServer(PmdContainerConfig config, int port) : base(config)
        {
            AsyncInit = true;

            _port = port;
            var ipHost = Dns.GetHostEntry("localhost");
            var ipAddr = ipHost.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddr, _port);

            _serverSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(localEndPoint);


            Framework.Logger.LogInformation("Listening on [{ip}:{port}]", ipAddr.MapToIPv4(), port);
        }

        public override async Task Init()
        {
            while (Running)
            {
                try
                {

                    _serverSocket.Listen(10);
                    var socket = await _serverSocket.AcceptAsync();
                    socket.SendTimeout = 1000;

                    var id = RegisterComponent<RokonServerConnection>(true, out var connection, socket);
                    OnClientConnected(connection, id);

                    _ = ProcessLoop(connection);

                }
                catch (SocketException ex)
                {
                    Framework.Logger.LogDebug("Listening loop threw error: ({exception})", ex.Message);
                }

            }
        }


        protected override byte[] ReceiveBytes(RokonServerConnection connection)
        {
            var buffer = new byte[1024];

            try
            {

                var result = connection.Socket.Receive(buffer);
                if (result == 0) connection.Cleanup();

                var actualData = new byte[result];
                Array.Copy(buffer, actualData, result);
                return actualData;

            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}:{port}] closed ({exception})", connection.GetEndpoint(), _port, ex.Message);
                connection.Cleanup();
            }

            return buffer;

        }

        protected override void SendBytes(RokonServerConnection connection, byte[] bytes)
        {
            try
            {

                lock (connection.SocketLock)
                {
                    connection.Socket.Send(bytes);
                }

            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}:{port}] closed ({exception})", connection.GetEndpoint(), _port, ex.Message);
                connection.Cleanup();
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
