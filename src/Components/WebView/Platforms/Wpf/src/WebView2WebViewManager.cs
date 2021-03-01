using System;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    // TODO: If we want to make this shareable across WPF and WinForms, we'll need to do so on a shared-source
    // basis. There can't be a single class for both, since the WebView2 classes have different identities on
    // the two platforms. But we could share the sources and use #if pragmas for any small variations that
    // exist between the two.

    internal class WebView2WebViewManager : WebViewManager
    {
        // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
        // making it substantially faster. Note that this isn't real HTTP traffic, since
        // we intercept all the requests within this origin.
        private const string AppOrigin = "http://0.0.0.0/";

        private readonly WebView2 _webview;
        private readonly Task _webviewReadyTask;

        public WebView2WebViewManager(WebView2 webview, IServiceProvider services, IFileProvider fileProvider)
            : base(services, WpfDispatcher.Instance, new Uri(AppOrigin), fileProvider)
        {
            _webview = webview ?? throw new ArgumentNullException(nameof(webview));

            // Unfortunately the CoreWebView2 can only be instantiated asynchronously.
            // We want the external API to behave as if initalization is synchronous,
            // so keep track of a task we can await during LoadUri.
            _webviewReadyTask = InitializeWebView2();
        }

        protected override void NavigateCore(Uri absoluteUri)
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

            _webview.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);
            _webview.CoreWebView2.WebResourceRequested += (sender, eventArgs) =>
            {
                if (TryGetResponseContent(eventArgs.Request.Uri, out var statusCode, out var statusMessage, out var content, out var headers))
                {
                    eventArgs.Response = environment.CreateWebResourceResponse(content, statusCode, statusMessage, headers);
                }
            };

            _webview.CoreWebView2.WebMessageReceived += (sender, eventArgs)
                => MessageReceived(new Uri(eventArgs.Source), eventArgs.TryGetWebMessageAsString());
        }
    }
}
