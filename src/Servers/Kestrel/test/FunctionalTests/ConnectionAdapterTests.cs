// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ConnectionAdapterTests : LoggedTest
    {
        [Fact]
        public async Task ThrowingSynchronousConnectionAdapterDoesNotCrashServer()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new ThrowingConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // Will throw because the exception in the connection adapter will close the connection.
                    await Assert.ThrowsAsync<IOException>(async () =>
                    {
                        await connection.Send(
                           "POST / HTTP/1.0",
                           "Content-Length: 1000",
                           "\r\n");

                        for (var i = 0; i < 1000; i++)
                        {
                            await connection.Send("a");
                            await Task.Delay(5);
                        }
                    });
                }
            }
        }

        private class ThrowingConnectionAdapter : IConnectionAdapter
        {
            public bool IsHttps => false;

            public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                throw new Exception();
            }
        }
    }
}
