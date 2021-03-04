using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Extensions.FileProviders;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    public sealed class BlazorWebView : Control, IDisposable
    {
        private WebView2Control _webview;
        private WebView2WebViewManager _webviewManager;

        private string _hostPage;
        private IServiceProvider _services;

        public BlazorWebView()
        {
            Dispatcher = new WindowsFormsDispatcher(this);
            RootComponents.CollectionChanged += HandleRootComponentsCollectionChanged;

            _webview = new WebView2Control()
            {
                Dock = DockStyle.Fill,
            };
            Controls.Add(_webview);
        }

        private WindowsFormsDispatcher Dispatcher { get; }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            StartWebViewCoreIfPossible();
        }

        public string HostPage
        {
            get => _hostPage;
            set
            {
                _hostPage = value;
                OnHostPagePropertyChanged();
            }
        }

        public ObservableCollection<RootComponent> RootComponents { get; } = new();

        public IServiceProvider Services
        {
            get => _services;
            set
            {
                _services = value;
                OnServicesPropertyChanged();
            }
        }

        private void OnHostPagePropertyChanged() => StartWebViewCoreIfPossible();

        private void OnServicesPropertyChanged() => StartWebViewCoreIfPossible();

        private bool RequiredStartupPropertiesSet =>
            Created &&
            _webview != null &&
            HostPage != null &&
            Services != null;

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

            _webviewManager = new WebView2WebViewManager(new WindowsFormsWebView2Wrapper(_webview), Services, Dispatcher, fileProvider, hostPageRelativePath);
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
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    var newItems = eventArgs.NewItems.Cast<RootComponent>();
                    var oldItems = eventArgs.OldItems.Cast<RootComponent>();

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _webviewManager?.Dispose();
                _webview?.Dispose();
            }
        }
    }
}
