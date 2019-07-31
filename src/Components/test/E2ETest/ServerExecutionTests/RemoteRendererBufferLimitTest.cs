// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Sdk;

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
            Client.RenderBatchReceived += (rendererId, id, data) => Batches.Add(new Batch(rendererId, id, data));
            Client.OnCircuitError += (error) => Errors.Add(error);

            Sink = _serverFixture.Host.Services.GetRequiredService<TestSink>();
            Sink.MessageLogged += LogMessages;
        }

        public BlazorClient Client { get; set; }

        private IList<Batch> Batches { get; set; } = new List<Batch>();

        // We use a stack so that we can search the logs in reverse order
        private ConcurrentStack<LogMessage> Logs { get; set; } = new ConcurrentStack<LogMessage>();

        private IList<string> Errors { get; set; } = new List<string>();

        public TestSink Sink { get; private set; }

        [Fact]
        public async Task NoNewBatchesAreCreated_WhenThereAreNoPendingRenderRequestsFromComponents()
        {
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            // Act
            await Client.SelectAsync("test-selector-select", "BasicTestApp.LimitCounterComponent");

            // Assert
            Assert.Equal(2, Batches.Count);
            Assert.Contains(Logs.ToArray(), l => (LogLevel.Debug, "No pending component render requests.") == (l.LogLevel, l.Message));
        }

        [Fact]
        public async Task NotAcknowledgingRenders_ProducesBatches_UpToTheLimit()
        {
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            // Act
            Client.ConfirmRenderBatch = false;
            var tcs = new TaskCompletionSource<object>();
            void SignalOnBatchNumber(int batchId, int rendererId, byte[] data)
            {
                // We wait for the batch Id 11 because its 1 batch for the initial render + 10 after we stop
                // unacknowledging batches.
                if (batchId == 11)
                {
                    tcs.TrySetResult(null);
                }
            }
            Client.RenderBatchReceived += SignalOnBatchNumber;

            await Client.SelectAsync("test-selector-select", "BasicTestApp.HighFrequencyComponent");

            await Task.WhenAny(Task.Delay(1000), tcs.Task);

            // Assert
            // We check that the value is 9 because the ticker starts at 0, there is a max buffer of 10 unacknowledged
            // batches and we don't acknowledge the initial batch to render the ticker component.
            Assert.Contains(Client.FindElementById("tick-value").Children, c => c is TextNode tn && tn.TextContent == "9");
            Assert.Equal(11, Batches.Count);
        }

        [Fact]
        public async Task ProducesNewBatch_WhenABatchGetsAcknowledged()
        {
            // Arrange
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            Client.ConfirmRenderBatch = false;
            var tcs = new TaskCompletionSource<object>();
            void SignalOnBatchNumber(int batchId, int rendererId, byte[] data)
            {
                // We wait for the batch Id 11 because its 1 batch for the initial render + 10 after we stop
                // unacknowledging batches.
                if (batchId == 11)
                {
                    tcs.TrySetResult(null);
                }
            }
            Client.RenderBatchReceived += SignalOnBatchNumber;

            await Client.SelectAsync("test-selector-select", "BasicTestApp.HighFrequencyComponent");

            await Task.WhenAny(Task.Delay(1000), tcs.Task);

            Assert.Contains(Client.FindElementById("tick-value").Children, c => c is TextNode tn && tn.TextContent == "9");
            Assert.Equal(11, Batches.Count);

            // Act
            await Client.ExpectRenderBatch(() => Client.ConfirmBatch(3)); // This is the batch after the initial batch

            // Assert
            Assert.Contains(
                Client.FindElementById("tick-value").Children,
                c => c is TextNode tn && int.Parse(tn.TextContent) >= 10);
            Assert.Equal(12, Batches.Count);
        }

        [Fact]
        public async Task ContinuesProducingBatches_UntilItStopsReceivingAcksAndResumesWhenItReceivesAcksAgain()
        {
            // Arrange
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
            Assert.Single(Batches);

            await Client.SelectAsync("test-selector-select", "BasicTestApp.HighFrequencyComponent");
            await Task.Delay(100);

            var tcs = new TaskCompletionSource<object>();
            var foundLogMessage = false;
            void SignalOnFullQueue(WriteContext context)
            {
                // We wait for the batch Id 11 because its 1 batch for the initial render + 10 after we stop
                // unacknowledging batches.
                if (context.Message == "The queue of unacknowledged render batches is full.")
                {
                    tcs.TrySetResult(null);
                    foundLogMessage = true;
                }
            }

            Sink.MessageLogged+= SignalOnFullQueue;
            Client.ConfirmRenderBatch = false; // This will stop acknowledging batches, so we should see a queue full message.

            await Task.WhenAny(Task.Delay(1000), tcs.Task);

            Sink.MessageLogged -= SignalOnFullQueue;
            Assert.True(foundLogMessage, "Log entry 'The queue of unacknowledged render batches is full.' not found.");

            var currentCount = Batches.Count;
            var lastBatchId = Batches[^1].Id;
            var lastRenderedValue = ((TextNode)Client.FindElementById("tick-value").Children.Single()).TextContent;
            // Act
            Client.ConfirmRenderBatch = true;

            // This will resume the render batches.
            await Client.ExpectRenderBatch(() => Client.ConfirmBatch(lastBatchId));

            // An indeterminate amount of renders will happen in this time.
            await Task.Delay(100);

            Sink.MessageLogged += SignalOnFullQueue;
            tcs = new TaskCompletionSource<object>();
            foundLogMessage = false;
            Logs.Clear();

            // This will cause the renders to stop.
            Client.ConfirmRenderBatch = false;
            await Task.WhenAny(Task.Delay(1000), tcs.Task);

            Sink.MessageLogged -= SignalOnFullQueue;
            Assert.True(foundLogMessage, "Log entry 'The queue of unacknowledged render batches is full.' not found.");

            // Assert
            Assert.True(lastBatchId + 10 < Batches[^1].Id, "We didn't produce more than 10 batches");

            var finalRenderedValue = ((TextNode)Client.FindElementById("tick-value").Children.Single()).TextContent;
            Assert.True(int.Parse(finalRenderedValue) > int.Parse(lastRenderedValue), "Ticker count didn't increase enough");
        }

        [Fact]
        public async Task DispatchedEventsWillKeepBeingProcessed_ButUpdatedWillBeDelayedUntilARenderIsAcknowledged()
        {
            // Arrange
            var baseUri = new Uri(_serverFixture.RootUri, "/subdir");
            Assert.True(await Client.ConnectAsync(baseUri, prerendered: false), "Couldn't connect to the app");
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

            Assert.Single(Logs, l => (LogLevel.Debug, "The queue of unacknowledged render batches is full.") == (l.LogLevel, l.Message));
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
            public Batch(int rendererId, int id, byte[] data)
            {
                Id = id;
                RendererId = rendererId;
                Data = data;
            }

            public int Id { get; }
            public int RendererId { get; }
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
