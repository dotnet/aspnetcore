// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    public class RemoteRendererTest : HtmlRendererTestBase
    {
        protected override HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider)
        {
            return GetRemoteRenderer(serviceProvider, CircuitClientProxy.OfflineClient);
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
            Assert.Equal(2, renderer.OfflineRenderBatches.Count);
        }

        [Fact]
        public async Task ProcessBufferedRenderBatches_WritesRenders()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var renderIds = new List<int>();

            var initialClient = new Mock<IClientProxy>();
            initialClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback((string name, object[] value, CancellationToken token) =>
                {
                    renderIds.Add((int)value[1]);
                })
                .Returns(Task.CompletedTask);
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
                .Callback((string name, object[] value, CancellationToken token) =>
                {
                    renderIds.Add((int)value[1]);
                })
                .Returns(Task.CompletedTask);
            var componentId = renderer.AssignRootComponentId(component);
            component.TriggerRender();
            renderer.OnRenderCompleted(1, null);

            circuitClient.SetDisconnected();
            component.TriggerRender();
            component.TriggerRender();

            // Act
            circuitClient.Transfer(client.Object, "new-connection");
            var task = renderer.ProcessBufferedRenderBatches();
            foreach (var id in renderIds)
            {
                renderer.OnRenderCompleted(id, null);
            }
            await task;

            // Assert
            client.Verify(c => c.SendCoreAsync("JS.RenderBatch", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private RemoteRenderer GetRemoteRenderer(IServiceProvider serviceProvider, CircuitClientProxy circuitClientProxy)
        {
            return new RemoteRenderer(
                serviceProvider,
                new RendererRegistry(),
                Mock.Of<IJSRuntime>(),
                circuitClientProxy,
                Dispatcher,
                HtmlEncoder.Default,
                NullLogger.Instance);
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;
            private RenderFragment _renderFragment;

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
    }
}
