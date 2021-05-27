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
    internal class WindowsFormsWebView2Wrapper : IWebView2Wrapper<WebView2Control, CoreWebView2Environment>
    {
        private readonly WindowsFormsCoreWebView2Wrapper _coreWebView2Wrapper;
        private CoreWebView2Environment _environment;

        public WindowsFormsWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            WebView2 = webView2;
            _coreWebView2Wrapper = new WindowsFormsCoreWebView2Wrapper(this);
        }

        public ICoreWebView2Wrapper<CoreWebView2Environment> CoreWebView2 => _coreWebView2Wrapper;

        public async Task<ICoreWebView2EnvironmentWrapper<CoreWebView2Environment>> CreateEnvironmentAsync()
        {
            return new WindowsFormsCoreWebView2EnvironmentWrapper(await CoreWebView2Environment.CreateAsync());
        }


        public Uri Source
        {
            get => WebView2.Source;
            set => WebView2.Source = value;
        }

        public WebView2Control WebView2 { get; }

        public CoreWebView2Environment Environment => _environment;

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

        public Task EnsureCoreWebView2Async(ICoreWebView2EnvironmentWrapper<CoreWebView2Environment> environment)
        {
            _environment = environment.CoreWebView2Environment;
            return WebView2.EnsureCoreWebView2Async(environment.CoreWebView2Environment);
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

    internal class WindowsFormsCoreWebView2Wrapper : ICoreWebView2Wrapper<CoreWebView2Environment>
    {
        private readonly WindowsFormsWebView2Wrapper _webView2;

        public WindowsFormsCoreWebView2Wrapper(WindowsFormsWebView2Wrapper webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            _webView2 = webView2;
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
