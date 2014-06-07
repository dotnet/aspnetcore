using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Server.Kestrel;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestralTests
{
    /// <summary>
    /// Summary description for EngineTests
    /// </summary>
    public class EngineTests
    {
        private async Task App(object callContext)
        {
            var request = callContext as IHttpRequestFeature;
            var response = callContext as IHttpResponseFeature;
            for (; ;)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        [Fact]
        public async Task EngineCanStartAndStop()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            engine.Stop();
        }

        [Fact]
        public async Task ListenerCanCreateAndDispose()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);
            started.Dispose();
            engine.Stop();
        }


        [Fact]
        public async Task ConnectionCanReadAndWrite()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 4001));
            socket.Send(Encoding.ASCII.GetBytes("POST / HTTP/1.0\r\n\r\nHello World"));
            socket.Shutdown(SocketShutdown.Send);
            var buffer = new byte[8192];
            for (; ;)
            {
                var length = socket.Receive(buffer);
                if (length == 0) { break; }
                var text = Encoding.ASCII.GetString(buffer, 0, length);
            }
            started.Dispose();
            engine.Stop();
        }
    }
}
