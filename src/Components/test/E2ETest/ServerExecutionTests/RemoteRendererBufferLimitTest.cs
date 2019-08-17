// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class RemoteRendererBufferLimitTest : IClassFixture<AspNetSiteServerFixture>, IDisposable
    {
        private static readonly TimeSpan DefaultLatencyTimeout = Debugger.IsAttached ? TimeSpan.FromSeconds(60) : TimeSpan.FromMilliseconds(500);

        private AspNetSiteServerFixture _serverFixture;

        public RemoteRendererBufferLimitTest(AspNetSiteServerFixture serverFixture)
        {
            serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            _serverFixture = serverFixture;

            // Needed here for side-effects
            _ = _serverFixture.RootUri;

            Client = new BlazorClient() { DefaultLatencyTimeout = DefaultLatencyTimeout };
            Client.RenderBatchReceived += (id, data) => Batches.Add(new Batch(id, data));

            Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            Sink.MessageLogged += LogMessages;
        }

        public BlazorClient Client { get; set; }

        private IList<Batch> Batches { get; set; } = new List<Batch>();

        // We use a stack so that we can search the logs in reverse order
        private ConcurrentStack<LogMessage> Logs { get; set; } = new ConcurrentStack<LogMessage>();

        public TestSink Sink { get; private set; }

        [Fact]
        public async Task DispatchedEventsWillKeepBeingProcessed_ButUpdatedWillBeDelayedUntilARenderIsAcknowledged()
        {
            // Arrange
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri), "Couldn't connect to the app");
            Assert.Single(Batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.LimitCounterComponent");
            Client.ConfirmRenderBatch = false;

            for (int i = 0; i < 10; i++)
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
            await Client.ExpectRenderBatch(() => Client.ConfirmBatch(Batches[^1].Id));

            // Assert
            Assert.Equal("12", ((TextNode)Client.FindElementById("the-count").Children.Single()).TextContent);
            Assert.Equal(fullCount + 1, Batches.Count);
        }

        private void LogMessages(WriteContext context) => Logs.Push(new LogMessage(context.LogLevel, context.Message, context.Exception));

        [DebuggerDisplay("{Message,nq}")]
        private class LogMessage
        {
            public LogMessage(LogLevel logLevel, string message, Exception exception)
            {
                LogLevel = logLevel;
                Message = message;
                Exception = exception;
            }

            public LogLevel LogLevel { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }

        private class Batch
        {
            public Batch(int id, byte[] data)
            {
                Id = id;
                Data = data;
            }

            public int Id { get; }
            public byte[] Data { get; }
        }

        public void Dispose()
        {
            if (Sink != null)
            {
                Sink.MessageLogged -= LogMessages;
            }
        }
    }
}
