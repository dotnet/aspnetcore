// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino
{
    internal class PhotinoWebViewManager : WebViewManager
    {
        private readonly PhotinoWindow _window;

        // On Windows, we can't use a custom scheme to host the initial HTML,
        // because webview2 won't let you do top-level navigation to such a URL.
        // On Linux/Mac, we must use a custom scheme, because their webviews
        // don't have a way to intercept http:// scheme requests.
        internal readonly static string BlazorAppScheme = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "http"
            : "app";

        internal readonly static string AppBaseUri
            = $"{BlazorAppScheme}://0.0.0.0/";

        public PhotinoWebViewManager(PhotinoWindow window, IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, string hostPageRelativePath)
            : base(provider, dispatcher, appBaseUri, fileProvider, hostPageRelativePath)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public Stream? HandleWebRequest(string url, out string? contentType)
        {
            if (url.StartsWith(AppBaseUri, StringComparison.Ordinal)
                && TryGetResponseContent(url, true, out var statusCode, out var statusMessage, out var content, out var headers))
            {
                headers.TryGetValue("Content-Type", out contentType);
                return content;
            }
            else
            {
                contentType = default;
                return null;
            }
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            _window.Load(absoluteUri);
        }

        protected override void SendMessage(string message)
        {
            throw new NotImplementedException();
        }
    }
}
