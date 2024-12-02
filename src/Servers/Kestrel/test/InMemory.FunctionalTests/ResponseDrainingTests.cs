// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ResponseDrainingTests : TestApplicationErrorLoggerLoggedTest
{
    public static TheoryData<ListenOptions> ConnectionMiddlewareData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)).UsePassThrough()
        };

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareData))]
    public async Task ConnectionClosedWhenResponseNotDrainedAtMinimumDataRate(ListenOptions listenOptions)
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var minRate = new MinDataRate(16384, TimeSpan.FromSeconds(2));

        await using (var server = new TestServer(context =>
        {
            context.Features.Get<IHttpMinResponseDataRateFeature>().MinDataRate = minRate;
            return Task.CompletedTask;
        }, testContext, listenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var transportConnection = connection.TransportConnection;

                var outputBufferedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

#pragma warning disable 0618 // TODO: Repalce OnWriterCompleted
                transportConnection.Output.OnWriterCompleted((ex, state) =>
                {
                    ((TaskCompletionSource)state).SetResult();
                },
                outputBufferedTcs);
#pragma warning restore

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "Connection: close",
                    "",
                    "");

                // Wait for the drain timeout to be set.
                await outputBufferedTcs.Task.DefaultTimeout();

                // Advance the clock to the grace period
                for (var i = 0; i < 2; i++)
                {
                    testContext.FakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                    testContext.ConnectionManager.OnHeartbeat();
                }

                testContext.FakeTimeProvider.Advance(Heartbeat.Interval - TimeSpan.FromSeconds(.5));
                testContext.ConnectionManager.OnHeartbeat();

                Assert.Null(transportConnection.AbortReason);

                testContext.FakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                testContext.ConnectionManager.OnHeartbeat();

                Assert.NotNull(transportConnection.AbortReason);
                Assert.Equal(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied, transportConnection.AbortReason.Message);

                Assert.Single(LogMessages, w => w.EventId.Id == 28 && w.LogLevel <= LogLevel.Debug);
            }
        }
    }
}
