using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Server.Kestrel;
using System;
using System.IO;
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
        private async Task AppChunked(object callContext)
        {
            var request = callContext as IHttpRequestFeature;
            var response = callContext as IHttpResponseFeature;
            response.Headers["Transfer-Encoding"] = new[] { "chunked" };
            for (; ;)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var hex = Encoding.ASCII.GetBytes(count.ToString("x") + "\r\n");
                await response.Body.WriteAsync(hex, 0, hex.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
                await response.Body.WriteAsync(new[] { (byte)'\r', (byte)'\n' }, 0, 2);
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

        [Fact]
        public async Task Http10()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);

            Transceive(
@"POST / HTTP/1.0

Hello World",
@"HTTP/1.0 200 OK

Hello World");
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10ContentLength()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);

            Transceive(
@"POST / HTTP/1.0
Content-Length: 5

Hello World",
@"HTTP/1.0 200 OK

Hello");
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10TransferEncoding()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);

            Transceive(
@"POST / HTTP/1.0
Transfer-Encoding: chunked

5
Hello
6
 World
0
ignored",
@"HTTP/1.0 200 OK

Hello World");
            started.Dispose();
            engine.Stop();
        }


        [Fact]
        public async Task Http10KeepAlive()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            Transceive(
@"GET / HTTP/1.0
Connection: Keep-Alive

POST / HTTP/1.0

Goodbye",
@"HTTP/1.0 200 OK
Transfer-Encoding: chunked
Connection: keep-alive

0
HTTP/1.0 200 OK
Transfer-Encoding: chunked

7
Goodbye
0
");
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10KeepAliveContentLength()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            Transceive(
@"POST / HTTP/1.0
Connection: Keep-Alive
Content-Length: 11

Hello WorldPOST / HTTP/1.0

Goodbye",
@"HTTP/1.0 200 OK
Transfer-Encoding: chunked
Connection: keep-alive

b
Hello World
0
HTTP/1.0 200 OK
Transfer-Encoding: chunked

7
Goodbye
0
");
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10KeepAliveTransferEncoding()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            Transceive(
@"POST / HTTP/1.0
Transfer-Encoding: chunked
Connection: keep-alive

5
Hello
6
 World
0
POST / HTTP/1.0

Goodbye",
@"HTTP/1.0 200 OK
Transfer-Encoding: chunked
Connection: keep-alive

b
Hello World
0
HTTP/1.0 200 OK
Transfer-Encoding: chunked

7
Goodbye
0
");
            started.Dispose();
            engine.Stop();
        }


        private void Transceive(string send, string expected)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 4001));

            var stream = new NetworkStream(socket, false);
            var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write(send);
            writer.Flush();
            stream.Flush();
            socket.Shutdown(SocketShutdown.Send);

            var reader = new StreamReader(stream, Encoding.ASCII);
            var actual = reader.ReadToEnd();

            Assert.Equal(expected, actual);
        }
    }
}
