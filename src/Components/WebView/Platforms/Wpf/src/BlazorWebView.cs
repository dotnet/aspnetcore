using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    public sealed class BlazorWebView : Control, IDisposable
    {
        private const string webViewTemplateChildName = "WebView";

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

            var webview = (WebView2)GetTemplateChild(webViewTemplateChildName);
            webview.Source = new Uri("https://microsoft.com");
        }

        public void Dispose()
        {
        }
    }
}
