using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.TestCommon
{
    public static class PortManager
    {
        public static int _nextPort = 8001;
        public static object _portLock = new object();
        
        public static int GetNextPort()
        {
            lock (_portLock)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    while (true)
                    {
                        try
                        {
                            var port = _nextPort++;
                            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                            return port;
                        }
                        catch (SocketException)
                        {
                            // Retry unless exhausted
                            if (_nextPort == 65536)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }
    }
}