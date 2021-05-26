// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    internal class WindowsFormsWebView2Wrapper : IWebView2Wrapper<WebView2Control, CoreWebView2Environment>
    {
        private readonly WindowsFormsCoreWebView2Wrapper _coreWebView2Wrapper;

        public WindowsFormsWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            WebView2 = webView2;
            _coreWebView2Wrapper = new WindowsFormsCoreWebView2Wrapper(WebView2);
        }

        public ICoreWebView2Wrapper CoreWebView2 => _coreWebView2Wrapper;

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

        public Task EnsureCoreWebView2Async(ICoreWebView2EnvironmentWrapper<CoreWebView2Environment> environment = null)
        {
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

    internal class WindowsFormsCoreWebView2Wrapper : ICoreWebView2Wrapper
    {
        private readonly WebView2Control _webView2;

        public WindowsFormsCoreWebView2Wrapper(WebView2Control webView2)
        {
            if (webView2 is null)
            {
                throw new ArgumentNullException(nameof(webView2));
            }

            _webView2 = webView2;
        }

        public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper ResourceContext)
        {
            _webView2.CoreWebView2.AddWebResourceRequestedFilter(uri, (CoreWebView2WebResourceContext)ResourceContext);
        }

        public Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> eventHandler)
        {
            EventHandler<CoreWebView2WebResourceRequestedEventArgs> realHandler = (object sender, CoreWebView2WebResourceRequestedEventArgs e) =>
            {
                eventHandler(_webView2, new WindowsFormsCoreWebView2AcceleratorKeyPressedEventArgsWrapper(e));
            };
            _webView2.CoreWebView2.WebResourceRequested += realHandler;

            // Return removal callback
            return () => { _webView2.CoreWebView2.WebResourceRequested -= realHandler; };
        }

        public void PostWebMessageAsString(string message)
        {
            _webView2.CoreWebView2.PostWebMessageAsString(message);
        }
    }
}
