using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    public class HeadlessWebView : WebViewManager, IDisposable
    {
        public HeadlessWebView(IServiceProvider provider) :
            base(provider)
        {
        }

        public override void Initialize(string baseUrl, string currentUrl)
        {
            base.Initialize(baseUrl, currentUrl);
            Host = (HeadlessWebViewHost)Provider.GetService<WebViewHost>();
            WebViewBrowser = Provider.GetService<WebViewBrowser>();
        }

        public HeadlessWebViewHost Host { get; private set; }

        public WebViewBrowser WebViewBrowser { get; private set; }

    }
}
