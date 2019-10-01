// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web.Rendering
{
    public class RemoteRendererTest
    {
        // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
        // failures.
        private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

        [Fact]
        public void WritesAreBufferedWhenTheClientIsOffline()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderer = GetRemoteRenderer(serviceProvider);
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
            Assert.Equal(2, renderer._unacknowledgedRenderBatches.Count);
        }

        [Fact]
        public void NotAcknowledgingRenders_ProducesBatches_UpToTheLimit()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderer = GetRemoteRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            for (int i = 0; i < 20; i++)
            {
                component.TriggerRender();

            }

            // Assert
            Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);
        }

        [Fact]
        public async Task NoNewBatchesAreCreated_WhenThereAreNoPendingRenderRequestsFromComponents()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderer = GetRemoteRenderer(serviceProvider);
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            for (var i = 0; i < 10; i++)
            {
                component.TriggerRender();
            }

            await renderer.OnRenderCompletedAsync(2, null);

            // Assert
            Assert.Equal(9, renderer._unacknowledgedRenderBatches.Count);
        }


        [Fact]
        public async Task ProducesNewBatch_WhenABatchGetsAcknowledged()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderer = GetRemoteRenderer(serviceProvider);
            var i = 0;
            var component = new TestComponent(builder =>
            {
                builder.AddContent(0, $"Value {i}");
            });

            // Act
            var componentId = renderer.AssignRootComponentId(component);
            for (i = 0; i < 20; i++)
            {
                component.TriggerRender();
            }
            Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);

            await renderer.OnRenderCompletedAsync(2, null);

            // Assert
            Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);
        }

        [Fact]
        public async Task ProcessBufferedRenderBatches_WritesRenders()
        {
            // Arrange
            var @event = new ManualResetEventSlim();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderIds = new List<long>();

            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var thirdBatchTCS = new TaskCompletionSource<object>();

            var initialClient = new Mock<IClientProxy>();
            initialClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[0]))
                .Returns(firstBatchTCS.Task);
            var circuitClient = new CircuitClientProxy(initialClient.Object, "connection0");
            var renderer = GetRemoteRenderer(serviceProvider, circuitClient);
            var component = new TestComponent(builder =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            });

            var client = new Mock<IClientProxy>();
            client.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[0]))
                .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[0] == 3 ? secondBatchTCS.Task : thirdBatchTCS.Task);

            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            _ = renderer.OnRenderCompletedAsync(2, null);

            @event.Reset();
            firstBatchTCS.SetResult(null);

            // Waiting is required here because the continuations of SetResult will not execute synchronously.
            @event.Wait(Timeout);

            circuitClient.SetDisconnected();
            component.TriggerRender();
            component.TriggerRender();

            // Act
            circuitClient.Transfer(client.Object, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();

            foreach (var id in renderIds.ToArray())
            {
                _ = renderer.OnRenderCompletedAsync(id, null);
            }

            secondBatchTCS.SetResult(null);
            thirdBatchTCS.SetResult(null);

            // Assert
            Assert.Equal(new long[] { 2, 3, 4 }, renderIds);
            Assert.True(task.Wait(3000), "One or more render batches weren't acknowledged");

            await task;
        }

        [Fact]
        public async Task OnRenderCompletedAsync_DoesNotThrowWhenReceivedDuplicateAcks()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new Mock<IClientProxy>(MockBehavior.Strict).Object, "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var renderIds = new List<long>();
            var onlineClient = new Mock<IClientProxy>();
            onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
                .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

            // This produces the initial batch (id = 2)
            await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
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
            var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

            // Act
            offlineClient.Transfer(onlineClient.Object, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            // Receive the ack for the initial batch
            _ = renderer.OnRenderCompletedAsync(2, null);
            // Receive the ack for the second batch
            _ = renderer.OnRenderCompletedAsync(3, null);

            firstBatchTCS.SetResult(null);
            secondBatchTCS.SetResult(null);
            // Repeat the ack for the third batch
            _ = renderer.OnRenderCompletedAsync(3, null);

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task OnRenderCompletedAsync_DoesNotThrowWhenThereAreNoPendingBatchesToAck()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var offlineClient = new CircuitClientProxy(new Mock<IClientProxy>(MockBehavior.Strict).Object, "offline-client");
            offlineClient.SetDisconnected();
            var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();
            var renderIds = new List<long>();
            var onlineClient = new Mock<IClientProxy>();
            onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
                .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

            // This produces the initial batch (id = 2)
            await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
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
            var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

            // Act
            offlineClient.Transfer(onlineClient.Object, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            // Receive the ack for the intial batch
            _ = renderer.OnRenderCompletedAsync(2, null);
            // Receive the ack for the second batch
            _ = renderer.OnRenderCompletedAsync(2, null);

            firstBatchTCS.SetResult(null);
            secondBatchTCS.SetResult(null);
            // Repeat the ack for the third batch
            _ = renderer.OnRenderCompletedAsync(3, null);

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ConsumesAllPendingBatchesWhenReceivingAHigherSequenceBatchId()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var renderIds = new List<long>();

            var onlineClient = new Mock<IClientProxy>();
            onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
                .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

            var renderer = GetRemoteRenderer(serviceProvider, new CircuitClientProxy(onlineClient.Object, "online-client"));
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();

            // This produces the initial batch (id = 2)
            await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
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
            var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

            // Act
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            // Pretend that we missed the ack for the initial batch
            _ = renderer.OnRenderCompletedAsync(3, null);
            firstBatchTCS.SetResult(null);
            secondBatchTCS.SetResult(null);

            // Assert
            Assert.Empty(exceptions);
            Assert.Empty(renderer._unacknowledgedRenderBatches);
        }

        [Fact]
        public async Task ThrowsIfWeReceivedAnAcknowledgeForANeverProducedBatch()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var firstBatchTCS = new TaskCompletionSource<object>();
            var secondBatchTCS = new TaskCompletionSource<object>();
            var renderIds = new List<long>();

            var onlineClient = new Mock<IClientProxy>();
            onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
                .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

            var renderer = GetRemoteRenderer(serviceProvider, new CircuitClientProxy(onlineClient.Object, "online-client"));
            RenderFragment initialContent = (builder) =>
            {
                builder.OpenElement(0, "my element");
                builder.AddContent(1, "some text");
                builder.CloseElement();
            };
            var trigger = new Trigger();

            // This produces the initial batch (id = 2)
            await renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
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
            var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

            // Act
            var exceptions = new List<Exception>();
            renderer.UnhandledException += (sender, e) =>
            {
                exceptions.Add(e);
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.OnRenderCompletedAsync(4, null));
            firstBatchTCS.SetResult(null);
            secondBatchTCS.SetResult(null);

            // Assert
            Assert.Equal(
                "Received an acknowledgement for batch with id '4' when the last batch produced was '3'.",
                exception.Message);
        }

        private TestRemoteRenderer GetRemoteRenderer(IServiceProvider serviceProvider, CircuitClientProxy circuitClient = null)
        {
            return new TestRemoteRenderer(
                serviceProvider,
                NullLoggerFactory.Instance,
                new CircuitOptions(),
                circuitClient ?? new CircuitClientProxy(),
                NullLogger.Instance);
        }

        private class TestRemoteRenderer : RemoteRenderer
        {
            public TestRemoteRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, CircuitOptions options, CircuitClientProxy client, ILogger logger)
                : base(serviceProvider, loggerFactory, options, client, logger)
            {
            }

            public async Task RenderComponentAsync<TComponent>(ParameterView initialParameters)
            {
                var component = InstantiateComponent(typeof(TComponent));
                var componentId = AssignRootComponentId(component);
                await RenderRootComponentAsync(componentId, initialParameters);
            }
        }

        private class TestComponent : IComponent, IHandleAfterRender
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

            public Action OnAfterRenderComplete { get; set; }

            public void Attach(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task OnAfterRenderAsync()
            {
                OnAfterRenderComplete?.Invoke();
                return Task.CompletedTask;
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                TriggerRender();
                return Task.CompletedTask;
            }

            public void TriggerRender()
            {
                var task = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(_renderFragment));
                Assert.True(task.IsCompletedSuccessfully);
            }
        }

        private class AutoParameterTestComponent : IComponent
        {
            private RenderHandle _renderHandle;

            [Parameter] public RenderFragment Content { get; set; }

            [Parameter] public Trigger Trigger { get; set; }

            public void Attach(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                Content = parameters.GetValueOrDefault<RenderFragment>(nameof(Content));
                Trigger ??= parameters.GetValueOrDefault<Trigger>(nameof(Trigger));
                Trigger.Component = this;
                TriggerRender();
                return Task.CompletedTask;
            }

            public void TriggerRender()
            {
                var task = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(Content));
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
    }
}
