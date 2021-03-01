using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    public sealed class BlazorWebView : Control, IDisposable
    {
        private const string webViewTemplateChildName = "WebView";
        private WebView2 _webview;
        private WebView2WebViewManager _webviewManager;

        public BlazorWebView()
        {
            Template = new ControlTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(WebView2), webViewTemplateChildName)
            };
        }

        public static readonly DependencyProperty ServicesProperty = DependencyProperty.Register(
            nameof(Services),
            typeof(IServiceProvider),
            typeof(BlazorWebView));

        public IServiceProvider Services
        {
            get => (IServiceProvider)GetValue(ServicesProperty);
            set => SetValue(ServicesProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // TODO: Can this be called more than once? We need the following only to happen once.
            _webview = (WebView2)GetTemplateChild(webViewTemplateChildName);
            _webviewManager = new WebView2WebViewManager(_webview, Services);
            _webviewManager.Navigate("/");
        }

        public void Dispose()
        {
            // TODO: Implement correct WPF disposal pattern
            _webviewManager?.Dispose();
            _webview?.Dispose();
        }
    }
}
