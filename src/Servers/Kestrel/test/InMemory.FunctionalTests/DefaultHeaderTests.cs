// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class DefaultHeaderTests : LoggedTest
{
    [Fact]
    public async Task TestDefaultHeaders()
    {
        var testContext = new TestServiceContext(LoggerFactory)
        {
            ServerOptions = { AddServerHeader = true }
        };

        await using (var server = new TestServer(ctx => Task.CompletedTask, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "GET / HTTP/1.0",
                    "",
                    "");

                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "Server: Kestrel",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "Server: Kestrel",
                    "",
                    "");
            }
        }
    }
}
