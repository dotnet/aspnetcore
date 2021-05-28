// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.WebView.WebView2;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
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
