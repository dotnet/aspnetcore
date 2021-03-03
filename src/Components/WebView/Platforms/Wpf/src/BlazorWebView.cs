using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
        public static readonly DependencyProperty HostPageProperty = DependencyProperty.Register(
            name: nameof(HostPage),
            propertyType: typeof(string),
            ownerType: typeof(BlazorWebView),
            typeMetadata: new PropertyMetadata(OnHostPagePropertyChanged));

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

            // TODO: Implement correct WPF disposal pattern, if this isn't already it
            Unloaded += (sender, eventArgs) => Dispose();
            Application.Current.Exit += HandleApplicationExiting;
        }

        public string HostPage
        {
            get => (string)GetValue(HostPageProperty);
            set => SetValue(HostPageProperty, value);
        }

        public ObservableCollection<RootComponent> RootComponents =>
            (ObservableCollection<RootComponent>)GetValue(RootComponentsProperty);

        public IServiceProvider Services
        {
            get => (IServiceProvider)GetValue(ServicesProperty);
            set => SetValue(ServicesProperty, value);
        }

        private static void OnServicesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BlazorWebView)d).OnServicesPropertyChanged(e);

        private void OnServicesPropertyChanged(DependencyPropertyChangedEventArgs e) => StartWebViewCoreIfPossible();

        private static void OnHostPagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BlazorWebView)d).OnHostPagePropertyChanged(e);

        private void OnHostPagePropertyChanged(DependencyPropertyChangedEventArgs e) => StartWebViewCoreIfPossible();

        private bool RequiredStartupPropertiesSet =>
            _webview != null &&
            HostPage != null &&
            Services != null;

        public override void OnApplyTemplate()
        {
            // Called when the control is created after its child control (the WebView2) is created from the Template property
            base.OnApplyTemplate();

            if (_webview == null)
            {
                _webview = (WebView2Control)GetTemplateChild(webViewTemplateChildName);
                StartWebViewCoreIfPossible();
            }
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

            // We assume the host page is always in the root of the content directory, because it's
            // unclear there's any other use case. We can add more options later if so.
            var contentRootDir = Path.GetDirectoryName(Path.GetFullPath(HostPage));
            var hostPageRelativePath = Path.GetRelativePath(contentRootDir, HostPage);
            var fileProvider = new PhysicalFileProvider(contentRootDir);

            _webviewManager = new WebView2WebViewManager(new WpfWeb2ViewWrapper(_webview), Services, WpfDispatcher.Instance, fileProvider, hostPageRelativePath);
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

                    foreach (var item in oldItems.Except(newItems))
                    {
                        await item.RemoveFromWebViewManagerAsync(_webviewManager);
                    }
                });
            }
        }

        private void HandleApplicationExiting(object sender, ExitEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            Application.Current.Exit -= HandleApplicationExiting;
            _webviewManager?.Dispose();
            _webview?.Dispose();
        }
    }
}
