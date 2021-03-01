using System;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WebView2WebViewManager : WebViewManager
    {
        private readonly WebView2 _webview;

        public WebView2WebViewManager(WebView2 webview, IServiceProvider services)
            : base(services, WpfDispatcher.Instance, "http://0.0.0.0")
        {
            _webview = webview ?? throw new ArgumentNullException(nameof(webview));
        }

        public override void Navigate(string url)
            => _webview.Source = new Uri(url);

        protected override void SendMessage(string message)
            => _webview.CoreWebView2.PostWebMessageAsString(message);
    }
}
