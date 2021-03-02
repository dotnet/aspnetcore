using System;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    public class WebView2WebViewManager : WebViewManager
    {
        // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
        // making it substantially faster. Note that this isn't real HTTP traffic, since
        // we intercept all the requests within this origin.
        private const string AppOrigin = "https://0.0.0.0/";

        private readonly IWebView2Wrapper _webview;
        private readonly Task _webviewReadyTask;

        public WebView2WebViewManager(IWebView2Wrapper webview, IServiceProvider services, Dispatcher dispatcher, IFileProvider fileProvider)
            : base(services, dispatcher, new Uri(AppOrigin), fileProvider)
        {
            _webview = webview ?? throw new ArgumentNullException(nameof(webview));

            // Unfortunately the CoreWebView2 can only be instantiated asynchronously.
            // We want the external API to behave as if initalization is synchronous,
            // so keep track of a task we can await during LoadUri.
            _webviewReadyTask = InitializeWebView2();
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            _ = Dispatcher.InvokeAsync(async () =>
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

            _webview.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);
            _webview.CoreWebView2.WebResourceRequested += (sender, eventArgs) =>
            {
                if (TryGetResponseContent(eventArgs.Request.Uri, out var statusCode, out var statusMessage, out var content, out var headers))
                {
                    eventArgs.Response = environment.CreateWebResourceResponse(content, statusCode, statusMessage, headers);
                }
            };

            // The code inside blazor.webview.js is meant to be agnostic to specific webview technologies,
            // so the following is an adaptor from blazor.webview.js conventions to WebView2 APIs
            await _webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.external = {
                    sendMessage: message => {
                        window.chrome.webview.postMessage(message);
                    },
                    receiveMessage: callback => {
                        window.chrome.webview.addEventListener('message', e => callback(e.data));
                    }
                };
            ").ConfigureAwait(true);

            _webview.CoreWebView2.WebMessageReceived += (sender, eventArgs)
                => MessageReceived(new Uri(eventArgs.Source), eventArgs.TryGetWebMessageAsString());
        }
    }
}
