// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Extensions.FileProviders;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    /// <summary>
    /// A Windows Forms control for hosting Blazor web components locally in Windows desktop applications.
    /// </summary>
    public sealed class BlazorWebView : ContainerControl, IDisposable
    {
        private readonly WebView2Control _webview;
        private WebView2WebViewManager _webviewManager;
        private string _hostPage;
        private IServiceProvider _services;

        /// <summary>
        /// Creates a new instance of <see cref="BlazorWebView"/>.
        /// </summary>
        public BlazorWebView()
        {
            Dispatcher = new WindowsFormsDispatcher(this);
            RootComponents.CollectionChanged += HandleRootComponentsCollectionChanged;

            _webview = new WebView2Control()
            {
                Dock = DockStyle.Fill,
            };
            ((BlazorWebViewControlCollection)Controls).AddInternal(_webview);
        }

        /// <summary>
        /// Returns the inner <see cref="WebView2Control"/> used by this control.
        /// </summary>
        /// <remarks>
        /// Directly using some functionality of the inner web view can cause unexpected behavior because its behavior
        /// is controlled by the <see cref="BlazorWebView"/> that is hosting it.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public WebView2Control WebView => _webview;

        private WindowsFormsDispatcher Dispatcher { get; }

        /// <inheritdoc />
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // NOTE: This only checks if THIS control is in design mode by existing on the design surface. If this
            // control wants to handle the case where it is a child control of another user control or custom
            // control then we might need to check parent tree manually to see if any parent is in DesignMode.
            if (DesignMode)
            {
                // TODO: Consider doing something in design mode, such as showing info about the control?
            }
            else
            {
                // Only boot up the WebView at runtime
                StartWebViewCoreIfPossible();
            }
        }

        /// <summary>
        /// Path to the host page within the application's static files. For example, <code>wwwroot\index.html</code>.
        /// This property must be set to a valid value for the Blazor components to start.
        /// </summary>
        public string HostPage
        {
            get => _hostPage;
            set
            {
                _hostPage = value;
                OnHostPagePropertyChanged();
            }
        }

        /// <summary>
        /// A collection of <see cref="RootComponent"/> instances that specify the Blazor <see cref="IComponent"/> types
        /// to be used directly in the specified <see cref="HostPage"/>.
        /// </summary>
        public ObservableCollection<RootComponent> RootComponents { get; } = new();

        /// <summary>
        /// Gets or sets an <see cref="IServiceProvider"/> containing services to be used by this control and also by application code.
        /// This property must be set to a valid value for the Blazor components to start.
        /// </summary>
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

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _webviewManager?.Dispose();
                _webview?.Dispose();
            }
        }

        /// <inheritdoc />
        protected override ControlCollection CreateControlsInstance()
        {
            return new BlazorWebViewControlCollection(this);
        }

        /// <summary>
        /// Custom control collection that ensures that only the owning <see cref="BlazorWebView"/> can add
        /// controls to it.
        /// </summary>
        private sealed class BlazorWebViewControlCollection : ControlCollection
        {
            public BlazorWebViewControlCollection(BlazorWebView owner) : base(owner)
            {
            }

            /// <summary>
            /// This is the only API we use; everything else is blocked.
            /// </summary>
            /// <param name="value"></param>
            internal void AddInternal(Control value) => base.Add(value);

            // Everything below is overridden to protect the control collection as read-only.
            public override bool IsReadOnly => true;

            public override void Add(Control value) => throw new NotSupportedException();
            public override void Clear() => throw new NotSupportedException();
            public override void Remove(Control value) => throw new NotSupportedException();
            public override void SetChildIndex(Control child, int newIndex) => throw new NotSupportedException();
        }
    }
}
