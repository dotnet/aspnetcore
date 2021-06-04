// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
}
