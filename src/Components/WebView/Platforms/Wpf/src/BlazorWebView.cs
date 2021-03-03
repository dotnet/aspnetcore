using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Extensions.FileProviders;
using WebView2Control = Microsoft.Web.WebView2.Wpf.WebView2;

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

        public static readonly DependencyProperty RootComponentsProperty = DependencyProperty.Register(
            name: nameof(RootComponents),
            propertyType: typeof(ObservableCollection<RootComponent>),
            ownerType: typeof(BlazorWebView));

        public static readonly DependencyProperty ServicesProperty = DependencyProperty.Register(
            name: nameof(Services),
            propertyType: typeof(IServiceProvider),
            ownerType: typeof(BlazorWebView),
            typeMetadata: new PropertyMetadata(OnServicesPropertyChanged));
        #endregion

        private const string webViewTemplateChildName = "WebView";
        private WebView2Control _webview;
        private WebView2WebViewManager _webviewManager;

        public BlazorWebView()
        {
            SetValue(RootComponentsProperty, new ObservableCollection<RootComponent>());
            RootComponents.CollectionChanged += HandleRootComponentsCollectionChanged;

            Template = new ControlTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(WebView2Control), webViewTemplateChildName)
            };
        }

        //public string HostPage
        //{
        //    get { return (string)GetValue(HostPageProperty); }
        //    set { SetValue(HostPageProperty, value); }
        //}

        public ObservableCollection<RootComponent> RootComponents =>
            (ObservableCollection<RootComponent>)GetValue(RootComponentsProperty);

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
            Services != null;

        public override void OnApplyTemplate()
        {
            // Called when the control is created after its child control (the WebView2) is created from the Template property

            base.OnApplyTemplate();

            // TODO: Can this be called more than once? We need the following only to happen once.
            _webview = (WebView2Control)GetTemplateChild(webViewTemplateChildName);

            StartWebViewCoreIfPossible();
        }

        protected override void OnInitialized(EventArgs e)
        {
            // Called when BeginInit/EndInit are used, such as when creating the control from XAML
            base.OnInitialized(e);

            StartWebViewCoreIfPossible();
        }

        private void StartWebViewCoreIfPossible()
        {
            if (!RequiredStartupPropertiesSet || _webviewManager != null)
            {
                return;
            }

            // TODO: Make content root configurable. Allow the developer to set the host page page.
            var appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var wwwroot = Path.Combine(appDir, "wwwroot");
            var fileProvider = new PhysicalFileProvider(Directory.Exists(wwwroot) ? wwwroot : appDir);

            // TODO: Can this get called multiple times, such as from OnApplyTemplate or a property change? Do we need to handle this more efficiently?
            _webviewManager = new WebView2WebViewManager(new WpfWeb2ViewWrapper(_webview), Services, WpfDispatcher.Instance, fileProvider);
            foreach (var rootComponent in RootComponents)
            {
                // Since the page isn't loaded yet, this will always complete synchronously
                _ = rootComponent.AddToWebViewManagerAsync(_webviewManager);
            }
            _webviewManager.Navigate("/");
        }

        private void HandleRootComponentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            // If we haven't initialized yet, this is a no-op
            if (_webviewManager != null)
            {
                // Dispatch because this is going to be async, and we want to catch any errors
                WpfDispatcher.Instance.InvokeAsync(async () =>
                {
                    var newItems = eventArgs.OldItems.Cast<RootComponent>();
                    var oldItems = eventArgs.NewItems.Cast<RootComponent>();

                    foreach (var item in newItems.Except(oldItems))
                    {
                        await item.AddToWebViewManagerAsync(_webviewManager);
                    }

                    foreach (var item in newItems.Except(oldItems))
                    {
                        await item.RemoveFromWebViewManagerAsync(_webviewManager);
                    }
                });
            }
        }

        public void Dispose()
        {
            // TODO: Implement correct WPF disposal pattern
            _webviewManager?.Dispose();
            _webview?.Dispose();
        }
    }
}
