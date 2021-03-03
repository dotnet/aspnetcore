using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.Wpf.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WpfWeb2ViewWrapper : IWebView2Wrapper
    {
        private readonly WebView2Control _webView2;
        private bool _hasInitialized;

        public WpfWeb2ViewWrapper(WebView2Control webView2)
        {
            _webView2 = webView2 ?? throw new ArgumentNullException(nameof(webView2));
        }

        public CoreWebView2 CoreWebView2 => _webView2.CoreWebView2;

        public Uri Source
        {
            get => _webView2.Source;
            set => _webView2.Source = value;
        }

        public Task EnsureCoreWebView2Async(CoreWebView2Environment environment = null)
        {
            if (_hasInitialized)
            {
                // We don't want people to think they can set more than one environment
                throw new InvalidOperationException($"{nameof(EnsureCoreWebView2Async)} may only be called once per control.");
            }

            _hasInitialized = true;
            return _webView2.EnsureCoreWebView2Async(environment);
        }
    }
}
