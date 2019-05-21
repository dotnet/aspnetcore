// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    public class RemoteRendererTest : HtmlRendererTestBase
    {
        protected override HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider)
        {
            return GetRemoteRenderer(serviceProvider, new CircuitClientProxy());
        }

        [Fact]
        public void WritesAreBufferedWhenTheClientIsOffline()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderer = (RemoteRenderer)GetHtmlRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            component.TriggerRender();

            // Assert
            Assert.Equal(2, renderer.PendingRenderBatches.Count);
        }

        [Fact]
        public async Task ProcessBufferedRenderBatches_WritesRenders()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var thirdBatchTCS = new TaskCompletionSource<object>();

            var initialClient = new TestClientProxy(new Dictionary<long, Task>
            {
                { 1, firstBatchTCS.Task },
            });
            var circuitClient = new CircuitClientProxy(initialClient, "connection0");
            var renderer = GetRemoteRenderer(serviceProvider, circuitClient);
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            var client = new TestClientProxy(new Dictionary<long, Task>
            {
                { 2, secondBatchTCS.Task },
                { 3, thirdBatchTCS.Task },
            });

            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            renderer.OnRenderCompleted(2, null);
            firstBatchTCS.SetResult(null);

            circuitClient.SetDisconnected();
            component.TriggerRender();
            component.TriggerRender();

            // Act
            circuitClient.Transfer(client, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();

            renderer.OnRenderCompleted(1, null);
            renderer.OnRenderCompleted(2, null);
            renderer.OnRenderCompleted(3, null);

            secondBatchTCS.SetResult(null);
            thirdBatchTCS.SetResult(null);

            // Assert
            Assert.True(task.Wait(3000), "One or more render batches werent acknowledged");

            await task;
        }

        [Fact]
        public async Task OnRenderCompletedAsync_CompletesTaskCorrespndingToReceviedBatch()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new TestClientProxy(), "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var onlineClient = new TestClientProxy(new Dictionary<long, Task>
            {
                { 1, firstBatchTCS.Task },
                { 2, secondBatchTCS.Task },
            });

            // This produces the initial batch (id = 2)
            var result = await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            }));
            trigger.Component.Content = (builder) =>
            {
                builder.OpenElement(0, "offline element");
                builder.AddContent(1, "offline text");
                builder.CloseElement();
            };
            // This produces an additional batch (id = 3)
            trigger.TriggerRender();

            // Act
            offlineClient.Transfer(onlineClient, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            var pendingRenders = renderer.PendingRenderBatches.ToArray();
            Assert.Equal(2, pendingRenders.Length);

            renderer.OnRenderCompleted(1, null);

            // Assert
            Assert.Empty(exceptions);
            Assert.Contains(renderer.PendingRenderBatches, p => p.BatchId == 2); // BatchId = 2 should still be queued.

            Assert.True(pendingRenders.First(f => f.BatchId == 1).CompletionSource.Task.IsCompletedSuccessfully, "We expect first batch to be ACKed");
            Assert.False(pendingRenders.First(f => f.BatchId == 2).CompletionSource.Task.IsCompletedSuccessfully, "We expect second batch to still be queued.");
        }

        [Fact]
        public async Task OnRenderCompletedAsync_DoesNothing_IfReceivingOnRenderCompleteForPreviouslyAcknowledgedBatch()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new TestClientProxy(), "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var onlineClient = new TestClientProxy(new Dictionary<long, Task>
            {
                { 1, firstBatchTCS.Task },
                { 2, secondBatchTCS.Task },
            });

            // This produces the initial batch (id = 2)
            var result = await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            }));
            trigger.Component.Content = (builder) =>
            {
                builder.OpenElement(0, "offline element");
                builder.AddContent(1, "offline text");
                builder.CloseElement();
            };
            // This produces an additional batch (id = 3)
            trigger.TriggerRender();

            // Act
            offlineClient.Transfer(onlineClient, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            var pendingRenders = renderer.PendingRenderBatches.ToArray();
            Assert.Equal(2, pendingRenders.Length);

            renderer.OnRenderCompleted(1, null);

            // Assert
            Assert.Empty(exceptions);
            var render = Assert.Single(renderer.PendingRenderBatches);
            Assert.Equal(2, render.BatchId);

            // This should no-op
            renderer.OnRenderCompleted(1, null);
            Assert.Empty(exceptions);
            render = Assert.Single(renderer.PendingRenderBatches);
            Assert.Equal(2, render.BatchId);
        }

        [Fact]
        public async Task OnRenderCompletedAsync_AllowsOutOfSequenceACK()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new TestClientProxy(), "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var onlineClient = new TestClientProxy(new Dictionary<long, Task>
            {
                { 1, firstBatchTCS.Task },
                { 2, secondBatchTCS.Task },
            });

            // This produces the initial batch (id = 2)
            var result = await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            }));
            trigger.Component.Content = (builder) =>
            {
                builder.OpenElement(0, "offline element");
                builder.AddContent(1, "offline text");
                builder.CloseElement();
            };
            // This produces an additional batch (id = 3)
            trigger.TriggerRender();

            // Act
            offlineClient.Transfer(onlineClient, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            var pendingRenders = renderer.PendingRenderBatches.ToArray();
            Assert.Equal(2, pendingRenders.Length);

            // Pretend that we missed the ack for the initial batch (batchId = 1)
            renderer.OnRenderCompleted(2, null);

            // Assert
            Assert.Empty(exceptions);
            Assert.Empty(renderer.PendingRenderBatches);

            Assert.True(pendingRenders.First(f => f.BatchId == 1).CompletionSource.Task.IsCompletedSuccessfully, "We expect first batch to be ACKed");
            Assert.True(pendingRenders.First(f => f.BatchId == 2).CompletionSource.Task.IsCompletedSuccessfully, "We expect second batch to be ACKed");
        }

        [Fact]
        public async Task ThrowsIfWeReceiveAnUnexpectedClientAcknowledge()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new TestClientProxy(), "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var onlineClient = new TestClientProxy(new Dictionary<long, Task>
            {
                { 1, firstBatchTCS.Task },
                { 2, secondBatchTCS.Task },
            });

            // This produces the initial batch (id = 2)
            var result = await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            }));
            trigger.Component.Content = (builder) =>
            {
                builder.OpenElement(0, "offline element");
                builder.AddContent(1, "offline text");
                builder.CloseElement();
            };
            // This produces an additional batch (id = 2)
            trigger.TriggerRender();
            var originallyQueuedBatches = renderer.PendingRenderBatches.Count;
            Assert.Equal(2, originallyQueuedBatches);

            // Act
            offlineClient.Transfer(onlineClient, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            // We expect ACKs for batchId 1 or 2. Sending a batch that's later than either should result in an error.
            renderer.OnRenderCompleted(3, null);
            firstBatchTCS.SetResult(null);
            secondBatchTCS.SetResult(null);

            // Assert
            var exception = Assert.IsType<InvalidOperationException>(Assert.Single(exceptions));
            Assert.Equal("Received a notification for a rendered batch when not expecting it. Most recent entry: '2'. Actual batch id: '3'.", exception.Message);
        }

        [Fact]
        public async Task PrerendersMultipleComponentsSuccessfully()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var renderer = GetRemoteRenderer(
                serviceProvider,
                new CircuitClientProxy());

            // Act
            var first = await renderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty);
            var second = await renderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(0, first.ComponentId);
            Assert.Equal(1, second.ComponentId);
            Assert.Equal(2, renderer.PendingRenderBatches.Count);
        }

        private RemoteRenderer GetRemoteRenderer(IServiceProvider serviceProvider, CircuitClientProxy circuitClientProxy)
        {
            var jsRuntime = new TestJSRuntime();

            return new RemoteRenderer(
                serviceProvider,
                new RendererRegistry(),
                jsRuntime,
                circuitClientProxy,
                Dispatcher,
                HtmlEncoder.Default,
                NullLogger.Instance);
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;
            private RenderFragment _renderFragment = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };

            public TestComponent()
            {
            }

            public TestComponent(RenderFragment renderFragment)
            {
                _renderFragment = renderFragment;
            }

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                TriggerRender();
                return Task.CompletedTask;
            }

            public void TriggerRender()
            {
                var task = _renderHandle.Invoke(() => _renderHandle.Render(_renderFragment));
                Assert.True(task.IsCompletedSuccessfully);
            }
        }

        private class AutoParameterTestComponent : IComponent
        {
            private RenderHandle _renderHandle;

            [Parameter] public RenderFragment Content { get; set; }

            [Parameter] public Trigger Trigger { get; set; }

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                Content = parameters.GetValueOrDefault<RenderFragment>(nameof(Content));
                Trigger ??= parameters.GetValueOrDefault<Trigger>(nameof(Trigger));
                Trigger.Component = this;
                TriggerRender();
                return Task.CompletedTask;
            }

            public void TriggerRender()
            {
                var task = _renderHandle.Invoke(() => _renderHandle.Render(Content));
                Assert.True(task.IsCompletedSuccessfully);
            }
        }

        private class Trigger
        {
            public AutoParameterTestComponent Component { get; set; }
            public void TriggerRender()
            {
                Component.TriggerRender();
            }
        }

        private class TestClientProxy : IClientProxy
        {
            private readonly Dictionary<long, Task> _results;

            public TestClientProxy(Dictionary<long, Task> results = null)
            {
                _results = results ?? new Dictionary<long, Task>();
            }

            public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            {
                var id = (long)args[1];
                return _results[id];
            }
        }

        private class TestJSRuntime : IJSRuntime
        {
            public Task<T> InvokeAsync<T>(string identifier, params object[] args) => Task.FromResult<T>(default);

            public void UntrackObjectRef(DotNetObjectRef dotNetObjectRef)
            {
                throw new NotImplementedException();
            }
        }
    }
}
