using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PmdThing.Core;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

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
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _serverSocket.Bind(localEndPoint);
 

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

                    _serverSocket.Listen(10);
                    var socket = _serverSocket.Accept();

                    var id = Register<RokonServerConnection>(true, out var connection, socket);
                    OnClientConnected(connection, id);

                    var processThread = new Thread(() => ProcessLoop(connection))
                    {
                        Name = $"Process ({socket.RemoteEndPoint})"
                    };
                    var sendThread = new Thread(() => SendLoop(connection))
                    {
                        Name = $"Send ({socket.RemoteEndPoint})"
                    };
                    var receiveThread = new Thread(() => ReceiveLoop(connection))
                    {
                        Name = $"Receive ({socket.RemoteEndPoint})"
                    };

                    processThread.Start();
                    sendThread.Start();
                    receiveThread.Start();

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
                    var result = connection.Socket.Receive(buffer);
                    if (result == 0) connection.Cleanup();

                    var actualData = new byte[result];
                    Array.Copy(buffer, actualData, result);
                    return actualData;
                }
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
                if (connection.Running)
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
