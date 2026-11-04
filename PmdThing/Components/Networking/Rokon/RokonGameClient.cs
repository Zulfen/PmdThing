using Microsoft.Extensions.Logging;
using PmdThing.Core;
using System.Net;
using System.Net.Sockets;

namespace PmdThing.Components.Networking.Rokon
{
    public class RokonGameClient : GameClient<IPAddress>
    {
        private readonly int _port;
        private readonly IPAddress _ip;
        private Socket? _socket;

        private bool _socketClosed;
        private readonly Lock _socketLock = new();

        public RokonGameClient(PmdContainerConfig config, string host, int port) : base(config)
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
                _socket = new Socket(_ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.NoDelay = true;

                try
                {

                    await _socket.ConnectAsync(_ip, _port);
                    _socket.SendTimeout = 1000;

                    OnConnect();
                    _ = Task.Run(ProcessLoop);

                    await base.Init();

                }
                catch (SocketException ex)
                {
                    Framework.Logger.LogWarning("Failed to connect to [{address}:{port}]: {exception}", GetEndpoint(), _port, ex.Message);
                    _socket.Close();
                }
            }
        }

        public override IPAddress GetEndpoint()
        {
            return _ip;
        }

        public override void Cleanup()
        {
            lock (_socketLock)
            {
                if (!_socketClosed)
                {
                    base.Cleanup();
                    _socket?.Shutdown(SocketShutdown.Both);
                    _socket?.Close(1000);

                    _socketClosed = true;
                }
            }
        }

        // https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/388
        protected override byte[] ReceiveBytes()
        {
            var buffer = new byte[1024];

            try
            {
                if (_socket?.Connected == true)
                {
                    var result = _socket.Receive(buffer);

                    if (result == 0)
                    {
                        _socketClosed = true;
                        Cleanup();
                    }

                    var actualData = new byte[result];
                    Array.Copy(buffer, actualData, result);
                    return actualData;
                }

            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", GetEndpoint(), ex.Message);
                Cleanup();
            }

            return buffer;
        }

        protected override void SendBytes(byte[] bytes)
        {
            try
            {
                lock (_socketLock)
                {
                    _socket?.Send(bytes);
                }
                
            }
            catch (SocketException ex)
            {
                Framework.Logger.LogDebug("Connection to [{address}] closed ({exception})", GetEndpoint(), ex.Message);
                Cleanup();
            }
        }

        public override bool IsNetworkConnected()
        {
            return !(_socket?.Poll(1, SelectMode.SelectRead) == true && _socket?.Available == 0);
        }
    }
}
