// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.Wpf.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WpfWebView2Wrapper : IWebView2Wrapper
    {
        private readonly WpfCoreWebView2Wrapper _coreWebView2Wrapper;

        public WpfWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            WebView2 = webView2;
            _coreWebView2Wrapper = new WpfCoreWebView2Wrapper(this);
        }

        public ICoreWebView2Wrapper CoreWebView2 => _coreWebView2Wrapper;

        public Uri Source
        {
            get => WebView2.Source;
            set => WebView2.Source = value;
        }

        public WebView2Control WebView2 { get; }

        public CoreWebView2Environment Environment { get; set; }

        public Action AddAcceleratorKeyPressedHandler(EventHandler<ICoreWebView2AcceleratorKeyPressedEventArgsWrapper> eventHandler)
        {
            EventHandler<CoreWebView2AcceleratorKeyPressedEventArgs> realHandler = (object sender, CoreWebView2AcceleratorKeyPressedEventArgs e) =>
            {
                eventHandler(WebView2, new WpfCoreWebView2AcceleratorKeyPressedEventArgsWrapper(e));
            };
            WebView2.AcceleratorKeyPressed += realHandler;

            // Return removal callback
            return () => { WebView2.AcceleratorKeyPressed -= realHandler; };
        }

        public async Task CreateEnvironmentAsync()
        {
            Environment = await CoreWebView2Environment.CreateAsync();
        }

        public Task EnsureCoreWebView2Async()
        {
            return WebView2.EnsureCoreWebView2Async(Environment);
        }
    }

    internal class WpfCoreWebView2AcceleratorKeyPressedEventArgsWrapper : ICoreWebView2AcceleratorKeyPressedEventArgsWrapper
    {
        private readonly CoreWebView2AcceleratorKeyPressedEventArgs _eventArgs;

        public WpfCoreWebView2AcceleratorKeyPressedEventArgsWrapper(CoreWebView2AcceleratorKeyPressedEventArgs eventArgs)
        {
            _eventArgs = eventArgs;
        }
        public uint VirtualKey => _eventArgs.VirtualKey;

        public int KeyEventLParam => _eventArgs.KeyEventLParam;

        public bool Handled
        {
            get => _eventArgs.Handled;
            set => _eventArgs.Handled = value;
        }
    }

    internal class WpfCoreWebView2Wrapper : ICoreWebView2Wrapper
    {
        private readonly WpfWebView2Wrapper _webView2;
        private WpfCoreWebView2SettingsWrapper _settings;

        public WpfCoreWebView2Wrapper(WpfWebView2Wrapper webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            _webView2 = webView2;
        }

        public ICoreWebView2SettingsWrapper Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new WpfCoreWebView2SettingsWrapper(_webView2.WebView2.CoreWebView2.Settings);
                }
                return _settings;
            }
        }

        public Task AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)
        {
            return _webView2.WebView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(javaScript);
        }

        public Action AddWebMessageReceivedHandler(Action<WebMessageReceivedEventArgs> messageReceivedHandler)
        {
            void nativeEventHandler(object sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                messageReceivedHandler(new WebMessageReceivedEventArgs(e.Source, e.TryGetWebMessageAsString()));
            }

            _webView2.WebView2.CoreWebView2.WebMessageReceived += nativeEventHandler;

            // Return removal callback
            return () =>
            {
                _webView2.WebView2.CoreWebView2.WebMessageReceived -= nativeEventHandler;
            };
        }

        public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper resourceContext)
        {
            _webView2.WebView2.CoreWebView2.AddWebResourceRequestedFilter(uri, (CoreWebView2WebResourceContext)resourceContext);
        }

        public Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> webResourceRequestedHandler)
        {
            void nativeEventHandler(object sender, CoreWebView2WebResourceRequestedEventArgs e)
            {
                webResourceRequestedHandler(_webView2.WebView2, new WpfCoreWebView2WebResourceRequestedEventArgsWrapper(_webView2.Environment, e));
            }

            _webView2.WebView2.CoreWebView2.WebResourceRequested += nativeEventHandler;

            // Return removal callback
            return () =>
            {
                _webView2.WebView2.CoreWebView2.WebResourceRequested -= nativeEventHandler;
            };
        }

        public void PostWebMessageAsString(string webMessageAsString)
        {
            _webView2.WebView2.CoreWebView2.PostWebMessageAsString(webMessageAsString);
        }
    }

    internal class WpfCoreWebView2SettingsWrapper : ICoreWebView2SettingsWrapper
    {
        private readonly CoreWebView2Settings _settings;

        public WpfCoreWebView2SettingsWrapper(CoreWebView2Settings settings)
        {
            _settings = settings;
        }

        public bool IsScriptEnabled
        {
            get => _settings.IsScriptEnabled;
            set => _settings.IsScriptEnabled = value;
        }
        public bool IsWebMessageEnabled
        {
            get => _settings.IsWebMessageEnabled;
            set => _settings.IsWebMessageEnabled = value;
        }
        public bool AreDefaultScriptDialogsEnabled
        {
            get => _settings.AreDefaultScriptDialogsEnabled;
            set => _settings.AreDefaultScriptDialogsEnabled = value;
        }
        public bool IsStatusBarEnabled
        {
            get => _settings.IsStatusBarEnabled;
            set => _settings.IsStatusBarEnabled = value;
        }
        public bool AreDevToolsEnabled
        {
            get => _settings.AreDevToolsEnabled;
            set => _settings.AreDevToolsEnabled = value;
        }
        public bool AreDefaultContextMenusEnabled
        {
            get => _settings.AreDefaultContextMenusEnabled;
            set => _settings.AreDefaultContextMenusEnabled = value;
        }
        public bool AreHostObjectsAllowed
        {
            get => _settings.AreHostObjectsAllowed;
            set => _settings.AreHostObjectsAllowed = value;
        }
        public bool IsZoomControlEnabled
        {
            get => _settings.IsZoomControlEnabled;
            set => _settings.IsZoomControlEnabled = value;
        }
        public bool IsBuiltInErrorPageEnabled
        {
            get => _settings.IsBuiltInErrorPageEnabled;
            set => _settings.IsBuiltInErrorPageEnabled = value;
        }
    }

    internal class WpfCoreWebView2WebResourceRequestedEventArgsWrapper : ICoreWebView2WebResourceRequestedEventArgsWrapper
    {
        private readonly CoreWebView2Environment _environment;
        private readonly CoreWebView2WebResourceRequestedEventArgs _webResourceRequestedEventArgs;

        public WpfCoreWebView2WebResourceRequestedEventArgsWrapper(CoreWebView2Environment environment, CoreWebView2WebResourceRequestedEventArgs webResourceRequestedEventArgs)
        {
            _environment = environment;
            _webResourceRequestedEventArgs = webResourceRequestedEventArgs;

            Request = new WpfCoreWebView2WebResourceRequestWrapper(webResourceRequestedEventArgs);
            ResourceContext = (CoreWebView2WebResourceContextWrapper)webResourceRequestedEventArgs.ResourceContext;
        }

        public ICoreWebView2WebResourceRequestWrapper Request { get; }

        public CoreWebView2WebResourceContextWrapper ResourceContext { get; }

        public void SetResponse(Stream content, int statusCode, string statusMessage, string headerString)
        {
            _webResourceRequestedEventArgs.Response = _environment.CreateWebResourceResponse(content, statusCode, statusMessage, headerString);
        }
    }

    internal class WpfCoreWebView2WebResourceRequestWrapper : ICoreWebView2WebResourceRequestWrapper
    {
        private readonly CoreWebView2WebResourceRequestedEventArgs _webResourceRequestedEventArgs;

        public WpfCoreWebView2WebResourceRequestWrapper(CoreWebView2WebResourceRequestedEventArgs webResourceRequestedEventArgs)
        {
            _webResourceRequestedEventArgs = webResourceRequestedEventArgs;
        }

        public string Uri
        {
            get => _webResourceRequestedEventArgs.Request.Uri;
            set => _webResourceRequestedEventArgs.Request.Uri = value;
        }
        public string Method
        {
            get => _webResourceRequestedEventArgs.Request.Method;
            set => _webResourceRequestedEventArgs.Request.Method = value;
        }
    }
}
