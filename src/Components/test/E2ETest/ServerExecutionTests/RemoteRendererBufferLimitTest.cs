// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19666")]
    public class RemoteRendererBufferLimitTest : IgnitorTest<ServerStartup>
    {
        public RemoteRendererBufferLimitTest(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        [Fact]
        public async Task DispatchedEventsWillKeepBeingProcessed_ButUpdatedWillBeDelayedUntilARenderIsAcknowledged()
        {
            // Arrange
            var baseUri = new Uri(ServerFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
            Assert.Single(Batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.LimitCounterComponent");
            Client.ConfirmRenderBatch = false;

            for (var i = 0; i < 10; i++)
            {
                await Client.ClickAsync("increment");
            }
            await Client.ClickAsync("increment", expectRenderBatch: false);

            Assert.Single(Logs, l => (LogLevel.Debug, "The queue of unacknowledged render batches is full.") == (l.LogLevel, l.Message));
            Assert.Equal("10", ((TextNode)Client.FindElementById("the-count").Children.Single()).TextContent);
            var fullCount = Batches.Count;

            // Act
            await Client.ClickAsync("increment", expectRenderBatch: false);

            Assert.Contains(Logs, l => (LogLevel.Debug, "The queue of unacknowledged render batches is full.") == (l.LogLevel, l.Message));
            Assert.Equal(fullCount, Batches.Count);
            Client.ConfirmRenderBatch = true;

            // This will resume the render batches.
            await Client.ExpectRenderBatch(() => Client.ConfirmBatch(Batches.Last().Id));

            // Assert
            Assert.Equal("12", ((TextNode)Client.FindElementById("the-count").Children.Single()).TextContent);
            Assert.Equal(fullCount + 1, Batches.Count);
        }
    }
}
