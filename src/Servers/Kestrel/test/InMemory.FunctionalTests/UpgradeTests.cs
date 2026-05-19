// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Server.Kestrel.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class UpgradeTests : LoggedTest
{
    [Fact]
    public async Task ResponseThrowsAfterUpgrade()
    {
        var upgrade = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpUpgradeFeature>();
            var stream = await feature.UpgradeAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.Body.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, ex.Message);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.BodyWriter.WriteAsync(new byte[1]).AsTask());
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, ex.Message);

            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync("New protocol data");
                await writer.FlushAsync();
                await writer.DisposeAsync();
            }

            upgrade.TrySetResult();
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

        var upgrade = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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

                upgrade.TrySetResult();
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
        var upgradeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
    public async Task AcceptsRequestWithContentLengthAndUpgrade()
    {
        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpUpgradeFeature>();

            if (HttpMethods.IsPost(context.Request.Method))
            {
                Assert.False(feature.IsUpgradableRequest);
                Assert.Equal(1, context.Request.ContentLength);
                Assert.Equal(1, await context.Request.Body.ReadAsync(new byte[10], 0, 10));
            }
            else
            {
                Assert.True(feature.IsUpgradableRequest);
            }
        },
        new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send("POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "Connection: Upgrade",
                    "",
                    "A");

                await connection.Receive("HTTP/1.1 200 OK");
            }
        }
    }

    [Fact]
    public async Task AcceptsRequestWithNoContentLengthAndUpgrade()
    {
        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpUpgradeFeature>();
            Assert.True(feature.IsUpgradableRequest);

            if (HttpMethods.IsPost(context.Request.Method))
            {
                Assert.Equal(0, context.Request.ContentLength);
            }
            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[10], 0, 10));
        },
        new TestServiceContext(LoggerFactory)))
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
    public async Task AcceptsRequestWithChunkedEncodingAndUpgrade()
    {
        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpUpgradeFeature>();

            Assert.Null(context.Request.ContentLength);

            if (HttpMethods.IsPost(context.Request.Method))
            {
                Assert.False(feature.IsUpgradableRequest);
                Assert.Equal("chunked", context.Request.Headers.TransferEncoding);

                var length = await context.Request.Body.FillBufferUntilEndAsync(new byte[100]);
                Assert.Equal(11, length);
            }
            else
            {
                Assert.True(feature.IsUpgradableRequest);
            }
        },
        new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send("POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "Connection: Upgrade",
                    "",
                    "B", "Hello World",
                    "0",
                    "",
                    "");
                await connection.Receive("HTTP/1.1 200 OK");
            }
        }
    }

    [Fact]
    public async Task ThrowsWhenUpgradingNonUpgradableRequest()
    {
        var upgradeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
                upgradeTcs.TrySetResult();
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
        var upgradeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpUpgradeFeature>();
            var duplexStream = await feature.UpgradeAsync();

            try
            {
                await duplexStream.CopyToAsync(Stream.Null);
                appCompletedTcs.SetResult();
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

    [Fact]
    public async Task DoesNotThrowGivenCanceledReadResult()
    {
        var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new TestServer(async context =>
        {
            try
            {
                var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
                var duplexStream = await upgradeFeature.UpgradeAsync();

                // Kestrel will call Transport.Input.CancelPendingRead() during shutdown so idle connections
                // can wake up and shutdown gracefully. We manually call CancelPendingRead() to simulate this and
                // ensure the Stream returned by UpgradeAsync doesn't throw in this case.
                // https://github.com/dotnet/aspnetcore/issues/26482
                var connectionTransportFeature = context.Features.Get<IConnectionTransportFeature>();
                connectionTransportFeature.Transport.Input.CancelPendingRead();

                // Use ReadAsync() instead of CopyToAsync() for this test since IsCanceled is only checked in
                // HttpRequestStream.ReadAsync() and not HttpRequestStream.CopyToAsync()
                Assert.Equal(0, await duplexStream.ReadAsync(new byte[1]));
                appCompletedTcs.SetResult();
            }
            catch (Exception ex)
            {
                appCompletedTcs.SetException(ex);
                throw;
            }
        },
        new TestServiceContext(LoggerFactory));

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

    [Fact]
    public async Task DoesNotCloseConnectionWithout101Response()
    {
        var requestCount = 0;

        await using (var server = new TestServer(async context =>
        {
            if (requestCount++ > 0)
            {
                await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
            }
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendEmptyGetWithUpgrade();
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                await connection.SendEmptyGetWithUpgrade();
                await connection.Receive("HTTP/1.1 101 Switching Protocols",
                    "Connection: Upgrade",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }
}
