using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    internal class WindowsFormsWebView2Wrapper : IWebView2Wrapper
    {
        private readonly WebView2Control _webView2;

        public WindowsFormsWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            _webView2 = webView2;
        }

        public CoreWebView2 CoreWebView2 => _webView2.CoreWebView2;

        public Uri Source
        {
            get => _webView2.Source;
            set => _webView2.Source = value;
        }

        public event EventHandler<CoreWebView2AcceleratorKeyPressedEventArgs> AcceleratorKeyPressed
        {
            add => _webView2.AcceleratorKeyPressed += value;
            remove => _webView2.AcceleratorKeyPressed -= value;
        }

        public Task EnsureCoreWebView2Async(CoreWebView2Environment environment = null)
        {
            return _webView2.EnsureCoreWebView2Async(environment);
        }
    }
}
