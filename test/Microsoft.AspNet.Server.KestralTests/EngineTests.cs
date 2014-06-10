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

            var data = new MemoryStream();
            for (; ;)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                data.Write(buffer, 0, count);
            }
            var bytes = data.ToArray();
            response.Headers["Content-Length"] = new[] { bytes.Length.ToString() };
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
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
Content-Length: 11

Hello World",
@"HTTP/1.0 200 OK

Hello World");
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10TransferEncoding()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(App);

            using (var connection = new TestConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.0",
                    "Transfer-Encoding: chunked",
                    "",
                    "5", "Hello", "6", " World", "0\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.0 200 OK",
                    "",
                    "Hello World");
            }



            started.Dispose();
            engine.Stop();
        }


        [Fact]
        public async Task Http10KeepAlive()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            using (var connection = new TestConnection())
            {
                await connection.SendEnd(
                    "GET / HTTP/1.0",
                    "Connection: keep-alive",
                    "",
                    "POST / HTTP/1.0",
                    "",
                    "Goodbye");
                await connection.Receive(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 0",
                    "Connection: keep-alive",
                    "\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 7",
                    "",
                    "Goodbye");
            }

            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10KeepAliveContentLength()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            using (var connection = new TestConnection())
            {
                await connection.SendEnd(
                    "POST / HTTP/1.0",
                    "Connection: keep-alive",
                    "Content-Length: 11",
                    "",
                    "Hello WorldPOST / HTTP/1.0",
                    "",
                    "Goodbye");
                await connection.Receive(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 11",
                    "Connection: keep-alive",
                    "",
                    "Hello World");
                await connection.ReceiveEnd(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 7",
                    "",
                    "Goodbye");
            }
            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Http10KeepAliveTransferEncoding()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            using (var connection = new TestConnection())
            {
                await connection.SendEnd(
                    "POST / HTTP/1.0",
                    "Transfer-Encoding: chunked",
                    "Connection: keep-alive",
                    "",
                    "5", "Hello", "6", " World", "0",
                    "POST / HTTP/1.0",
                    "",
                    "Goodbye");
                await connection.Receive(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 11",
                    "Connection: keep-alive",
                    "",
                    "Hello World");
                await connection.ReceiveEnd(
                    "HTTP/1.0 200 OK",
                    "Content-Length: 7",
                    "",
                    "Goodbye");
            }

            started.Dispose();
            engine.Stop();
        }

        [Fact]
        public async Task Expect100ContinueForBody()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer(AppChunked);

            using (var connection = new TestConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Expect: 100-continue",
                    "Content-Length: 11",
                    "Connection: close",
                    "\r\n");
                await connection.Receive("HTTP/1.1 100 Continue", "\r\n");
                await connection.SendEnd("Hello World");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 11",
                    "Connection: close",
                    "", 
                    "Hello World");
            }

            started.Dispose();
            engine.Stop();
        }

        private void Transceive(string send, string expected)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 4001));

            var stream = new NetworkStream(socket, false);
            Task.Run(async () =>
            {
                try
                {
                    var writer = new StreamWriter(stream, Encoding.ASCII);
                    foreach (var ch in send)
                    {
                        await writer.WriteAsync(ch);
                        await writer.FlushAsync();
                        await Task.Delay(TimeSpan.FromMilliseconds(5));
                    }
                    writer.Flush();
                    stream.Flush();
                    socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                }
            });

            var reader = new StreamReader(stream, Encoding.ASCII);
            var actual = reader.ReadToEnd();

            Assert.Equal(expected, actual);
        }
    }
}
