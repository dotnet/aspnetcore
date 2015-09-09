using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class MultipleLoopTests
    {
        private readonly Libuv _uv;
        private readonly IKestrelTrace _logger = new KestrelTrace(new TestLogger());
        public MultipleLoopTests()
        {
            var engine = new KestrelEngine(LibraryManager, new ShutdownNotImplemented(), new TestLogger());
            _uv = engine.Libuv;
        }

        ILibraryManager LibraryManager
        {
            get
            {
                var locator = CallContextServiceLocator.Locator;
                if (locator == null)
                {
                    return null;
                }
                var services = locator.ServiceProvider;
                if (services == null)
                {
                    return null;
                }
                return (ILibraryManager)services.GetService(typeof(ILibraryManager));
            }
        }

        [Fact]
        public void InitAndCloseServerPipe()
        {
            var loop = new UvLoopHandle(_logger);
            var pipe = new UvPipeHandle(_logger);

            loop.Init(_uv);
            pipe.Init(loop, true);
            pipe.Bind(@"\\.\pipe\InitAndCloseServerPipe");
            pipe.Dispose();

            loop.Run();

            pipe.Dispose();
            loop.Dispose();

        }

        [Fact]
        public void ServerPipeListenForConnections()
        {
            var loop = new UvLoopHandle(_logger);
            var serverListenPipe = new UvPipeHandle(_logger);

            loop.Init(_uv);
            serverListenPipe.Init(loop, false);
            serverListenPipe.Bind(@"\\.\pipe\ServerPipeListenForConnections");
            serverListenPipe.Listen(128, (_1, status, error, _2) =>
            {
                var serverConnectionPipe = new UvPipeHandle(_logger);
                serverConnectionPipe.Init(loop, true);
                try
                {
                    serverListenPipe.Accept(serverConnectionPipe);
                }
                catch (Exception)
                {
                    serverConnectionPipe.Dispose();
                    return;
                }

                var writeRequest = new UvWriteReq(new KestrelTrace(new TestLogger()));
                writeRequest.Init(loop);
                writeRequest.Write(
                    serverConnectionPipe,
                    new ArraySegment<ArraySegment<byte>>(new ArraySegment<byte>[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) }),
                    (_3, status2, error2, _4) =>
                    {
                        writeRequest.Dispose();
                        serverConnectionPipe.Dispose();
                        serverListenPipe.Dispose();
                    },
                    null);

            }, null);

            var worker = new Thread(() =>
            {
                var loop2 = new UvLoopHandle(_logger);
                var clientConnectionPipe = new UvPipeHandle(_logger);
                var connect = new UvConnectRequest(new KestrelTrace(new TestLogger()));

                loop2.Init(_uv);
                clientConnectionPipe.Init(loop2, true);
                connect.Init(loop2);
                connect.Connect(clientConnectionPipe, @"\\.\pipe\ServerPipeListenForConnections", (_1, status, error, _2) =>
                {
                    var buf = loop2.Libuv.buf_init(Marshal.AllocHGlobal(8192), 8192);

                    connect.Dispose();
                    clientConnectionPipe.ReadStart(
                        (_3, cb, _4) => buf,
                        (_3, status2, error2, _4) =>
                        {
                            if (status2 == 0)
                            {
                                clientConnectionPipe.Dispose();
                            }
                        },
                        null);
                }, null);
                loop2.Run();
                loop2.Dispose();
            });
            worker.Start();
            loop.Run();
            loop.Dispose();
            worker.Join();
        }


        [Fact]
        public void ServerPipeDispatchConnections()
        {
            var pipeName = @"\\.\pipe\ServerPipeDispatchConnections" + Guid.NewGuid().ToString("n");

            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);

            var serverConnectionPipe = default(UvPipeHandle);
            var serverConnectionPipeAcceptedEvent = new ManualResetEvent(false);
            var serverConnectionTcpDisposedEvent = new ManualResetEvent(false);

            var serverListenPipe = new UvPipeHandle(_logger);
            serverListenPipe.Init(loop, false);
            serverListenPipe.Bind(pipeName);
            serverListenPipe.Listen(128, (_1, status, error, _2) =>
            {
                serverConnectionPipe = new UvPipeHandle(_logger);
                serverConnectionPipe.Init(loop, true);
                try
                {
                    serverListenPipe.Accept(serverConnectionPipe);
                    serverConnectionPipeAcceptedEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    serverConnectionPipe.Dispose();
                    serverConnectionPipe = null;
                }
            }, null);

            var serverListenTcp = new UvTcpHandle(_logger);
            serverListenTcp.Init(loop);
            serverListenTcp.Bind(new IPEndPoint(0, 54321));
            serverListenTcp.Listen(128, (_1, status, error, _2) =>
            {
                var serverConnectionTcp = new UvTcpHandle(_logger);
                serverConnectionTcp.Init(loop);
                serverListenTcp.Accept(serverConnectionTcp);

                serverConnectionPipeAcceptedEvent.WaitOne();

                var writeRequest = new UvWriteReq(new KestrelTrace(new TestLogger()));
                writeRequest.Init(loop);
                writeRequest.Write2(
                    serverConnectionPipe,
                    new ArraySegment<ArraySegment<byte>>(new ArraySegment<byte>[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) }),
                    serverConnectionTcp,
                    (_3, status2, error2, _4) =>
                    {
                        writeRequest.Dispose();
                        serverConnectionTcp.Dispose();
                        serverConnectionTcpDisposedEvent.Set();
                        serverConnectionPipe.Dispose();
                        serverListenPipe.Dispose();
                        serverListenTcp.Dispose();
                    },
                    null);
            }, null);

            var worker = new Thread(() =>
            {
                var loop2 = new UvLoopHandle(_logger);
                var clientConnectionPipe = new UvPipeHandle(_logger);
                var connect = new UvConnectRequest(new KestrelTrace(new TestLogger()));

                loop2.Init(_uv);
                clientConnectionPipe.Init(loop2, true);
                connect.Init(loop2);
                connect.Connect(clientConnectionPipe, pipeName, (_1, status, error, _2) =>
                {
                    connect.Dispose();

                    var buf = loop2.Libuv.buf_init(Marshal.AllocHGlobal(64), 64);

                    serverConnectionTcpDisposedEvent.WaitOne();

                    clientConnectionPipe.ReadStart(
                        (_3, cb, _4) => buf,
                        (_3, status2, error2, _4) =>
                        {
                            if (status2 == 0)
                            {
                                clientConnectionPipe.Dispose();
                                return;
                            }
                            var clientConnectionTcp = new UvTcpHandle(_logger);
                            clientConnectionTcp.Init(loop2);
                            clientConnectionPipe.Accept(clientConnectionTcp);
                            var buf2 = loop2.Libuv.buf_init(Marshal.AllocHGlobal(64), 64);
                            clientConnectionTcp.ReadStart(
                                (_5, cb, _6) => buf2,
                                (_5, status3, error3, _6) =>
                                {
                                    if (status3 == 0)
                                    {
                                        clientConnectionTcp.Dispose();
                                    }
                                },
                                null);
                        },
                        null);
                }, null);
                loop2.Run();
                loop2.Dispose();
            });

            var worker2 = new Thread(() =>
            {
                try
                {
                    serverConnectionPipeAcceptedEvent.WaitOne();

                    var socket = new Socket(SocketType.Stream, ProtocolType.IP);
                    socket.Connect(IPAddress.Loopback, 54321);
                    socket.Send(new byte[] { 6, 7, 8, 9 });
                    socket.Shutdown(SocketShutdown.Send);
                    var cb = socket.Receive(new byte[64]);
                    socket.Dispose();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            worker.Start();
            worker2.Start();

            loop.Run();
            loop.Dispose();
            worker.Join();
            worker2.Join();
        }
    }
}
