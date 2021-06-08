// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino
{
    internal class PhotinoWebViewManager : WebViewManager
    {
        private readonly PhotinoWindow _window;

        public PhotinoWebViewManager(PhotinoWindow window, IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, string hostPageRelativePath)
            : base(provider, dispatcher, appBaseUri, fileProvider, hostPageRelativePath)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            throw new NotImplementedException();
        }

        protected override void SendMessage(string message)
        {
            throw new NotImplementedException();
        }
    }
}
