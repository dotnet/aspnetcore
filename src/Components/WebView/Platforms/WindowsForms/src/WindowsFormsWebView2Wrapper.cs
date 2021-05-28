// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    internal class WindowsFormsWebView2Wrapper : IWebView2Wrapper
    {
        private readonly WindowsFormsCoreWebView2Wrapper _coreWebView2Wrapper;

        public WindowsFormsWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            WebView2 = webView2;
            _coreWebView2Wrapper = new WindowsFormsCoreWebView2Wrapper(this);
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
                eventHandler(WebView2, new WindowsFormsCoreWebView2AcceleratorKeyPressedEventArgsWrapper(e));
            };
            WebView2.AcceleratorKeyPressed += realHandler;

            // Return removal callback
            return () => { WebView2.AcceleratorKeyPressed -= realHandler; };
        }

        public async Task EstablishEnvironmentAsync()
        {
            //return new WindowsFormsCoreWebView2EnvironmentWrapper(await CoreWebView2Environment.CreateAsync());
            Environment = await CoreWebView2Environment.CreateAsync();
        }

        public Task EnsureCoreWebView2WithEstablishedEnvironmentAsync()
        {
            return WebView2.EnsureCoreWebView2Async(Environment);
        }
    }

    internal class WindowsFormsCoreWebView2AcceleratorKeyPressedEventArgsWrapper : ICoreWebView2AcceleratorKeyPressedEventArgsWrapper
    {
        private readonly CoreWebView2AcceleratorKeyPressedEventArgs _eventArgs;

        public WindowsFormsCoreWebView2AcceleratorKeyPressedEventArgsWrapper(CoreWebView2AcceleratorKeyPressedEventArgs eventArgs)
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

    internal class WindowsFormsCoreWebView2EnvironmentWrapper : ICoreWebView2EnvironmentWrapper<CoreWebView2Environment>
    {
        public WindowsFormsCoreWebView2EnvironmentWrapper(CoreWebView2Environment coreWebView2Environment)
        {
            if (coreWebView2Environment is null)
            {
                throw new ArgumentNullException(nameof(coreWebView2Environment));
            }

            CoreWebView2Environment = coreWebView2Environment;
        }

        public CoreWebView2Environment CoreWebView2Environment { get; }
    }

    internal class WindowsFormsCoreWebView2Wrapper : ICoreWebView2Wrapper
    {
        private readonly WindowsFormsWebView2Wrapper _webView2;
        private WindowsFormsCoreWebView2SettingsWrapper _settings;

        public WindowsFormsCoreWebView2Wrapper(WindowsFormsWebView2Wrapper webView2)
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
                    _settings = new WindowsFormsCoreWebView2SettingsWrapper(_webView2.WebView2.CoreWebView2.Settings);
                }
                return _settings;
            }
        }

        public Task AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)
        {
            return _webView2.WebView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(javaScript);
        }

        public void AddWebMessageReceivedHandler(Action<WebMessageReceivedEventArgs> messageReceived)
        {
            void eventHandler(object sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                messageReceived(new WebMessageReceivedEventArgs(e.Source, e.TryGetWebMessageAsString()));
            }

            _webView2.WebView2.CoreWebView2.WebMessageReceived += eventHandler;
        }

        public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper ResourceContext)
        {
            _webView2.WebView2.CoreWebView2.AddWebResourceRequestedFilter(uri, (CoreWebView2WebResourceContext)ResourceContext);
        }

        public Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> eventHandler)
        {
            EventHandler<CoreWebView2WebResourceRequestedEventArgs> realHandler = (object sender, CoreWebView2WebResourceRequestedEventArgs e) =>
            {
                eventHandler(_webView2.WebView2, new WindowsFormsCoreWebView2WebResourceRequestedEventArgsWrapper(_webView2.Environment, e));
            };
            _webView2.WebView2.CoreWebView2.WebResourceRequested += realHandler;

            // Return removal callback
            return () => { _webView2.WebView2.CoreWebView2.WebResourceRequested -= realHandler; };
        }

        public void PostWebMessageAsString(string message)
        {
            _webView2.WebView2.CoreWebView2.PostWebMessageAsString(message);
        }
    }

    internal class WindowsFormsCoreWebView2SettingsWrapper : ICoreWebView2SettingsWrapper
    {
        private CoreWebView2Settings _settings;

        public WindowsFormsCoreWebView2SettingsWrapper(CoreWebView2Settings settings)
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

    internal class WindowsFormsCoreWebView2WebResourceRequestedEventArgsWrapper : ICoreWebView2WebResourceRequestedEventArgsWrapper
    {
        private readonly CoreWebView2Environment _env;
        private readonly CoreWebView2WebResourceRequestedEventArgs _e;

        public WindowsFormsCoreWebView2WebResourceRequestedEventArgsWrapper(CoreWebView2Environment env, CoreWebView2WebResourceRequestedEventArgs e)
        {
            Request = new WindowsFormsCoreWebView2WebResourceRequestWrapper(e);
            ResourceContext = (CoreWebView2WebResourceContextWrapper)e.ResourceContext;
            _env = env;
            _e = e;
        }

        public ICoreWebView2WebResourceRequestWrapper Request { get; }

        public CoreWebView2WebResourceContextWrapper ResourceContext { get; }

        public void SetResponse(Stream content, int statusCode, string statusMessage, string headerString)
        {
            _e.Response = _env.CreateWebResourceResponse(content, statusCode, statusMessage, headerString);
        }
    }

    internal class WindowsFormsCoreWebView2WebResourceRequestWrapper : ICoreWebView2WebResourceRequestWrapper
    {
        private CoreWebView2WebResourceRequestedEventArgs _e;

        public WindowsFormsCoreWebView2WebResourceRequestWrapper(CoreWebView2WebResourceRequestedEventArgs e)
        {
            _e = e;
        }

        public string Uri
        {
            get => _e.Request.Uri;
            set => _e.Request.Uri = value;
        }
        public string Method
        {
            get => _e.Request.Method;
            set => _e.Request.Method = value;
        }
        public Stream Content
        {
            get => _e.Request.Content;
            set => _e.Request.Content = value;
        }
    }
}
