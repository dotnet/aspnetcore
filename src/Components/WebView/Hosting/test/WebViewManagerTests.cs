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
        public void CanStart()
        {
            // Arrange
            var provider = new ServiceCollection().AddTestBlazorWebView().BuildServiceProvider();
            var webViewManager = new TestWebViewManager(provider);
            webViewManager.SetUrls("https://localhost:5001/", "https://localhost:5001/");

            // Act
            webViewManager.Start();

            // Assert
            Assert.NotNull(webViewManager.GetCurrentScope());
        }
    }
}
