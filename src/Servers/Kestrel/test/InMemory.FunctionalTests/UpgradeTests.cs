// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Server.Kestrel.Tests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class UpgradeTests : LoggedTest
    {
        [Fact]
        public async Task ResponseThrowsAfterUpgrade()
        {
            var upgrade = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var server = new TestServer(async context =>
            {
                var feature = context.Features.Get<IHttpUpgradeFeature>();
                var stream = await feature.UpgradeAsync();

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.Body.WriteAsync(new byte[1], 0, 1));
                Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, ex.Message);

                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteLineAsync("New protocol data");
                    await writer.FlushAsync();
                    await writer.DisposeAsync();
                }

                upgrade.TrySetResult(true);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGetWithUpgrade();
                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");

                    await connection.Receive("New protocol data");
                    await upgrade.Task.DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RequestBodyAlwaysEmptyAfterUpgrade()
        {
            const string send = "Custom protocol send";
            const string recv = "Custom protocol recv";

            var upgrade = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var server = new TestServer(async context =>
            {
                try
                {
                    var feature = context.Features.Get<IHttpUpgradeFeature>();
                    var stream = await feature.UpgradeAsync();

                    var buffer = new byte[128];
                    var read = await context.Request.Body.ReadAsync(buffer, 0, 128).DefaultTimeout();
                    Assert.Equal(0, read);

                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream))
                    {
                        var line = await reader.ReadLineAsync();
                        Assert.Equal(send, line);
                        await writer.WriteLineAsync(recv);
                        await writer.FlushAsync();
                        await writer.DisposeAsync();
                    }

                    upgrade.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    upgrade.SetException(ex);
                    throw;
                }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGetWithUpgrade();

                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");

                    await connection.Send(send + "\r\n");
                    await connection.Receive(recv);

                    await upgrade.Task.DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task UpgradeCannotBeCalledMultipleTimes()
        {
            var upgradeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var server = new TestServer(async context =>
            {
                var feature = context.Features.Get<IHttpUpgradeFeature>();
                await feature.UpgradeAsync();

                try
                {
                    await feature.UpgradeAsync();
                }
                catch (Exception e)
                {
                    upgradeTcs.TrySetException(e);
                    throw;
                }

                while (!context.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(100);
                }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGetWithUpgrade();
                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");
                    await connection.WaitForConnectionClose();
                }
            }

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await upgradeTcs.Task.DefaultTimeout());
            Assert.Equal(CoreStrings.UpgradeCannotBeCalledMultipleTimes, ex.Message);
        }

        [Fact]
        public async Task RejectsRequestWithContentLengthAndUpgrade()
        {
            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send("POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 1",
                        "Connection: Upgrade",
                        "",
                        "");

                    await connection.ReceiveEnd(
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task AcceptsRequestWithNoContentLengthAndUpgrade()
        {
            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    await connection.SendEmptyGetWithUpgrade();
                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [Fact]
        public async Task RejectsRequestWithChunkedEncodingAndUpgrade()
        {
            await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send("POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "Connection: Upgrade",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ThrowsWhenUpgradingNonUpgradableRequest()
        {
            var upgradeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var server = new TestServer(async context =>
            {
                 var feature = context.Features.Get<IHttpUpgradeFeature>();
                 Assert.False(feature.IsUpgradableRequest);
                 try
                 {
                     var stream = await feature.UpgradeAsync();
                 }
                 catch (Exception e)
                 {
                     upgradeTcs.TrySetException(e);
                 }
                 finally
                 {
                     upgradeTcs.TrySetResult(false);
                 }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGet();
                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await upgradeTcs.Task).DefaultTimeout();
            Assert.Equal(CoreStrings.CannotUpgradeNonUpgradableRequest, ex.Message);
        }

        [Fact]
        public async Task RejectsUpgradeWhenLimitReached()
        {
            const int limit = 10;
            var upgradeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serviceContext = new TestServiceContext(LoggerFactory);
            serviceContext.ConnectionManager = new ConnectionManager(serviceContext.Log, ResourceCounter.Quota(limit));

            await using (var server = new TestServer(async context =>
            {
                var feature = context.Features.Get<IHttpUpgradeFeature>();
                if (feature.IsUpgradableRequest)
                {
                    try
                    {
                        var stream = await feature.UpgradeAsync();
                        while (!context.RequestAborted.IsCancellationRequested)
                        {
                            await Task.Delay(100);
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        upgradeTcs.TrySetException(ex);
                    }
                }
            }, serviceContext))
            {
                using (var disposables = new DisposableStack<InMemoryConnection>())
                {
                    for (var i = 0; i < limit; i++)
                    {
                        var connection = server.CreateConnection();
                        disposables.Push(connection);

                        await connection.SendEmptyGetWithUpgradeAndKeepAlive();
                        await connection.Receive("HTTP/1.1 101");
                    }

                    using (var connection = server.CreateConnection())
                    {
                        await connection.SendEmptyGetWithUpgradeAndKeepAlive();
                        await connection.Receive("HTTP/1.1 200");
                    }
                }
            }

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await upgradeTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(60)));
            Assert.Equal(CoreStrings.UpgradedConnectionLimitReached, exception.Message);
        }

        [Fact]
        public async Task DoesNotThrowOnFin()
        {
            var appCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async context =>
            {
                var feature = context.Features.Get<IHttpUpgradeFeature>();
                var duplexStream = await feature.UpgradeAsync();

                try
                {
                    await duplexStream.CopyToAsync(Stream.Null);
                    appCompletedTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    appCompletedTcs.SetException(ex);
                    throw;
                }

            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGetWithUpgrade();
                    await connection.Receive("HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");
                }

                await appCompletedTcs.Task.DefaultTimeout();
            }
        }
    }
}
