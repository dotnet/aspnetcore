// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class UpgradeTests
    {
        [Fact]
        public async Task ResponseThrowsAfterUpgrade()
        {
            var upgrade = new TaskCompletionSource<bool>();
            using (var server = new TestServer(async context =>
            {
                var feature = context.Features.Get<IHttpUpgradeFeature>();
                var stream = await feature.UpgradeAsync();

                var ex = Assert.Throws<InvalidOperationException>(() => context.Response.Body.WriteByte((byte)' '));
                Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, ex.Message);

                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("New protocol data");
                }

                upgrade.TrySetResult(true);
            }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send("GET / HTTP/1.1",
                        "Host:",
                        "Connection: Upgrade",
                        "",
                        "");
                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");

                    await connection.Receive("New protocol data");
                    await upgrade.Task.TimeoutAfter(TimeSpan.FromSeconds(30));
                }
            }
        }

        [Fact]
        public async Task RequestBodyAlwaysEmptyAfterUpgrade()
        {
            const string send = "Custom protocol send";
            const string recv = "Custom protocol recv";

            var upgrade = new TaskCompletionSource<bool>();
            using (var server = new TestServer(async context =>
            {
                try
                {
                    var feature = context.Features.Get<IHttpUpgradeFeature>();
                    var stream = await feature.UpgradeAsync();

                    var buffer = new byte[128];
                    var read = await context.Request.Body.ReadAsync(buffer, 0, 128).TimeoutAfter(TimeSpan.FromSeconds(10));
                    Assert.Equal(0, read);

                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream))
                    {
                        var line = await reader.ReadLineAsync();
                        Assert.Equal(send, line);
                        await writer.WriteLineAsync(recv);
                    }

                    upgrade.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    upgrade.SetException(ex);
                    throw;
                }
            }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send("GET / HTTP/1.1",
                        "Host:",
                        "Connection: Upgrade",
                        "",
                        "");

                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");

                    await connection.Send(send + "\r\n");
                    await connection.Receive(recv);

                    await upgrade.Task.TimeoutAfter(TimeSpan.FromSeconds(30));
                }
            }
        }

        [Fact]
        public async Task RejectsRequestWithContentLengthAndUpgrade()
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            using (var connection = server.CreateConnection())
            {
                await connection.Send("POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "Connection: Upgrade",
                    "",
                    "1");

                await connection.ReceiveForcedEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "Content-Length: 0",
                    "",
                    "");
            }
        }

        [Fact]
        public async Task AcceptsRequestWithNoContentLengthAndUpgrade()
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send("POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 0",
                        "Connection: Upgrade, keep-alive",
                        "",
                        "");
                    await connection.Receive("HTTP/1.1 200 OK");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send("GET / HTTP/1.1",
                        "Host:",
                        "Connection: Upgrade",
                        "",
                        "");
                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [Fact]
        public async Task RejectsRequestWithChunkedEncodingAndUpgrade()
        {
            using (var server = new TestServer(context => TaskCache.CompletedTask))
            using (var connection = server.CreateConnection())
            {
                await connection.Send("POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "Connection: Upgrade",
                    "",
                    "");
                await connection.ReceiveForcedEnd(
                    "HTTP/1.1 400 Bad Request",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "Content-Length: 0",
                    "",
                    "");
            }
        }
    }
}
