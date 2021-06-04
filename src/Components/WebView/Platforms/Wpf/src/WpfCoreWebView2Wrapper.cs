// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
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
}
