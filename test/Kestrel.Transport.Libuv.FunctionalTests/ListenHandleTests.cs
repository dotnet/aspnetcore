// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Listening to open TCP socket and/or pipe handles is not supported on Windows.")]
    public class ListenHandleTests : LoggedTest
    {
        [ConditionalFact]
        public async Task CanListenToOpenTcpSocketHandle()
        {
            using (var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                using (var server = new TestServer(_ => Task.CompletedTask, new TestServiceContext(LoggerFactory), new ListenOptions((ulong)listenSocket.Handle)))
                {
                    using (var connection = new TestConnection(((IPEndPoint)listenSocket.LocalEndPoint).Port))
                    {
                        await connection.SendEmptyGet();

                        await connection.Receive(
                            "HTTP/1.1 200 OK",
                            $"Date: {server.Context.DateHeaderValue}",
                            "Content-Length: 0",
                            "",
                            "");
                    }
                }
            }
        }
    }
}
