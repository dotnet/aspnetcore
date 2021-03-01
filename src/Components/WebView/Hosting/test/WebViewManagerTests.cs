using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class WebViewManagerTests
    {
        [Fact]
        public void RaisesOnPageAttachedWhenPageAttaches()
        {
            // Arrange
            var provider = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
            var webViewManager = new TestWebViewManager(provider);
            var didTriggerEvent = false;
            webViewManager.OnPageAttached += (sender, eventArgs) =>
            {
                Assert.Same(webViewManager, sender);
                didTriggerEvent = true;
            };

            // Act
            webViewManager.ReceiveInitializationMessage();

            // Assert
            Assert.True(didTriggerEvent);
        }

        [Fact]
        public void CanRenderRootComponent()
        {
            // Arrange
            var provider = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
            var webViewManager = new TestWebViewManager(provider);
            webViewManager.OnPageAttached += (sender, eventArgs) =>
                webViewManager.AddRootComponent(typeof(MyComponent), "#app", ParameterView.Empty);

            // Act
            Assert.Empty(webViewManager.SentIpcMessages);
            webViewManager.ReceiveInitializationMessage();

            // Assert
            Assert.Collection(webViewManager.SentIpcMessages,
                m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
                m => AssertHelpers.IsRenderBatch(m));
        }

        private class MyComponent : IComponent
        {
            private RenderHandle _handle;

            public void Attach(RenderHandle renderHandle)
            {
                _handle = renderHandle;
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                _handle.Render(builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddContent(1, "Hello world!");
                    builder.CloseElement();
                });

                return Task.CompletedTask;
            }
        }
    }
}
