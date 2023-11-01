// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

[OSSkipCondition(OperatingSystems.Windows, SkipReason = "Listening to open TCP socket and/or pipe handles is not supported on Windows.")]
public class ListenHandleTests : LoggedTest
{
    // The Socket.Handle will be passed into libuv, which calls close() on the file descriptor when TestServer is disposed.  If
    // the managed Socket is also disposed or finalized, it will try to call close() again on the file descriptor, which may lead to
    // race condition bugs (test hangs) if the file descriptor has been re-used for another resource.  In .NET Core, objects
    // assigned to static fields should never be disposed or finalized (even at process shutdown).
    // https://github.com/aspnet/KestrelHttpServer/issues/2597
    private static readonly Socket _canListenToOpenTcpSocketHandleSocket =
        new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    [ConditionalFact]
    public async Task CanListenToOpenTcpSocketHandle()
    {
        _canListenToOpenTcpSocketHandleSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        await using (var server = new TestServer(_ => Task.CompletedTask, new TestServiceContext(LoggerFactory),
            new ListenOptions((ulong)_canListenToOpenTcpSocketHandleSocket.Handle)))
        {
            using (var connection = new TestConnection(((IPEndPoint)_canListenToOpenTcpSocketHandleSocket.LocalEndPoint).Port))
            {
                await connection.SendEmptyGet();

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }
}

