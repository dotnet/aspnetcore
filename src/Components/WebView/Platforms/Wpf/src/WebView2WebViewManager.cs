using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WebView2WebViewManager : WebViewManager
    {
        private readonly WebView2 _webview;
        private readonly Task _webviewReadyTask;

        public WebView2WebViewManager(WebView2 webview, IServiceProvider services)
            : base(services, WpfDispatcher.Instance, new Uri("http://0.0.0.0"))
        {
            _webview = webview ?? throw new ArgumentNullException(nameof(webview));

            // Unfortunately the CoreWebView2 can only be instantiated asynchronously.
            // We want the external API to behave as if initalization is synchronous,
            // so keep track of a task we can await during LoadUri.
            _webviewReadyTask = InitializeWebView2();
        }

        protected override void LoadUri(Uri absoluteUri)
        {
            _ = WpfDispatcher.Instance.InvokeAsync(async () =>
            {
                await _webviewReadyTask;
                _webview.Source = absoluteUri;
            });
        }

        protected override void SendMessage(string message)
            => _webview.CoreWebView2.PostWebMessageAsString(message);

        private async Task InitializeWebView2()
        {
            var environment = await CoreWebView2Environment.CreateAsync().ConfigureAwait(true);
            await _webview.EnsureCoreWebView2Async(environment);

            _webview.CoreWebView2.WebMessageReceived += (sender, eventArgs)
                => MessageReceived(new Uri(eventArgs.Source), eventArgs.TryGetWebMessageAsString());
        }
    }
}
