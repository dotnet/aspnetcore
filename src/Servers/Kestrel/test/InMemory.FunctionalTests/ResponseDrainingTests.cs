// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ResponseDrainingTests : TestApplicationErrorLoggerLoggedTest
    {
        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionClosedWhenResponseNotDrainedAtMinimumDataRate(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var heartbeatManager = new HeartbeatManager(testContext.ConnectionManager);
            var minRate = new MinDataRate(16384, TimeSpan.FromSeconds(2));

            using (var server = new TestServer(context =>
            {
                context.Features.Get<IHttpMinResponseDataRateFeature>().MinDataRate = minRate;
                return Task.CompletedTask;
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    var transportConnection = connection.TransportConnection;

                    var outputBufferedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    transportConnection.Output.OnWriterCompleted((ex, state) =>
                    {
                        ((TaskCompletionSource<object>)state).SetResult(null);
                    }, outputBufferedTcs);

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
                        testContext.MockSystemClock.UtcNow += TimeSpan.FromSeconds(1);
                        heartbeatManager.OnHeartbeat(testContext.SystemClock.UtcNow);
                    }

                    testContext.MockSystemClock.UtcNow += Heartbeat.Interval - TimeSpan.FromSeconds(.5);
                    heartbeatManager.OnHeartbeat(testContext.SystemClock.UtcNow);

                    Assert.Null(transportConnection.AbortReason);

                    testContext.MockSystemClock.UtcNow += TimeSpan.FromSeconds(1);
                    heartbeatManager.OnHeartbeat(testContext.SystemClock.UtcNow);

                    Assert.NotNull(transportConnection.AbortReason);
                    Assert.Equal(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied, transportConnection.AbortReason.Message);

                    Assert.Single(TestApplicationErrorLogger.Messages, w => w.EventId.Id == 28 && w.LogLevel == LogLevel.Information);
                }
            }
        }
    }
}
