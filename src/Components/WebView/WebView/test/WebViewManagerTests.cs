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
            var services = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
            var fileProvider = new TestFileProvider();
            var webViewManager = new TestWebViewManager(services, fileProvider);
            var didTriggerEvent = false;
            webViewManager.OnPageAttached += (sender, eventArgs) =>
            {
                Assert.Same(webViewManager, sender);
                didTriggerEvent = true;
            };

            // Act
            webViewManager.ReceiveAttachPageMessage();

            // Assert
            Assert.True(didTriggerEvent);
        }

        [Fact]
        public void CanRenderRootComponent()
        {
            // Arrange
            var services = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
            var fileProvider = new TestFileProvider();
            var webViewManager = new TestWebViewManager(services, fileProvider);
            webViewManager.OnPageAttached += (sender, eventArgs) =>
                webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);

            // Act
            Assert.Empty(webViewManager.SentIpcMessages);
            webViewManager.ReceiveAttachPageMessage();

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
