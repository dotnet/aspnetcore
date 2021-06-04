// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
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
}
