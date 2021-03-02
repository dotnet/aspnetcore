using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Wpf;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    public sealed class BlazorWebView : Control, IDisposable
    {
        #region Dependency property definitions
        //public static readonly DependencyProperty HostPageProperty = DependencyProperty.Register(
        //    name: nameof(HostPage),
        //    propertyType: typeof(string),
        //    ownerType: typeof(BlazorWebView),
        //    typeMetadata: new PropertyMetadata(OnHostPagePropertyChanged));

        //public static readonly DependencyProperty RootComponentsProperty = DependencyProperty.Register(
        //    name: nameof(RootComponents),
        //    propertyType: typeof(ObservableCollection<RootComponent>),
        //    ownerType: typeof(BlazorWebView));

        public static readonly DependencyProperty ServicesProperty = DependencyProperty.Register(
            name: nameof(Services),
            propertyType: typeof(IServiceProvider),
            ownerType: typeof(BlazorWebView),
            typeMetadata: new PropertyMetadata(OnServicesPropertyChanged));
        #endregion

        private const string webViewTemplateChildName = "WebView";
        private WebView2 _webview;
        private WebView2WebViewManager _webviewManager;

        public BlazorWebView()
        {
            //SetValue(RootComponentsProperty, new ObservableCollection<RootComponent>());
            //RootComponents.CollectionChanged += (_, ___) => StartWebViewCoreIfPossible();

            Template = new ControlTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(WebView2), webViewTemplateChildName)
            };
        }

        //public string HostPage
        //{
        //    get { return (string)GetValue(HostPageProperty); }
        //    set { SetValue(HostPageProperty, value); }
        //}

        //public ObservableCollection<RootComponent> RootComponents => (ObservableCollection<RootComponent>)GetValue(RootComponentsProperty);

        public IServiceProvider Services
        {
            get { return (IServiceProvider)GetValue(ServicesProperty); }
            set { SetValue(ServicesProperty, value); }
        }

        private static void OnServicesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BlazorWebView)d).OnServicesPropertyChanged(e);

        private void OnServicesPropertyChanged(DependencyPropertyChangedEventArgs e) => StartWebViewCoreIfPossible();

        //private static void OnHostPagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BlazorWebView)d).OnHostPagePropertyChanged(e);

        //private void OnHostPagePropertyChanged(DependencyPropertyChangedEventArgs e) => StartWebViewCoreIfPossible();

        private bool RequiredStartupPropertiesSet =>
            _webview != null &&
            //HostPage != null &&
            //RootComponents.Any() &&
            Services != null;

        public override void OnApplyTemplate()
        {
            // Called when the control is created after its child control (the WebView2) is created from the Template property

            base.OnApplyTemplate();

            // TODO: Can this be called more than once? We need the following only to happen once.
            _webview = (WebView2)GetTemplateChild(webViewTemplateChildName);

            StartWebViewCoreIfPossible();
        }

        private void StartWebViewCoreIfPossible()
        {
            if (!RequiredStartupPropertiesSet)
            {
                return;
            }

            // TODO: Make content root configurable. Allow the developer to set the host page page.
            var appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var wwwroot = Path.Combine(appDir, "wwwroot");
            var fileProvider = new PhysicalFileProvider(Directory.Exists(wwwroot) ? wwwroot : appDir);

            // TODO: Can this get called multiple times, such as from OnApplyTemplate or a property change? Do we need to handle this more efficiently?
            _webviewManager = new WebView2WebViewManager(_webview, Services, fileProvider);
            _webviewManager.OnPageAttached += (sender, eventArgs) =>
            {
                _webviewManager.AddRootComponentAsync(typeof(TemporaryFakeStuff.DemoComponent), "#app", ParameterView.Empty);
            };
            _webviewManager.Navigate("/");
        }

        protected override void OnInitialized(EventArgs e)
        {
            // Called when BeginInit/EndInit are used, such as when creating the control from XAML
            base.OnInitialized(e);

            StartWebViewCoreIfPossible();
        }

        public void Dispose()
        {
            // TODO: Implement correct WPF disposal pattern
            _webviewManager?.Dispose();
            _webview?.Dispose();
        }
    }
}

// TODO: Replace with actual mechanism for setting a root component
namespace TemporaryFakeStuff
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;

    internal class DemoComponent : ComponentBase
    {
        int count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Hello, world!");
            builder.CloseElement();

            builder.OpenElement(2, "p");
            builder.AddContent(3, $"Current count: {count}");
            builder.CloseElement();

            builder.OpenElement(4, "button");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () =>
            {
                count++;
            }));
            builder.AddContent(6, "Click me");
            builder.CloseElement();
        }
    }
}
