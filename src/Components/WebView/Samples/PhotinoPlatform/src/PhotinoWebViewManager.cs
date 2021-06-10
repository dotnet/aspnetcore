// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
            _window.WebMessageReceived += (sender, message) =>
            {
                // On some platforms, we need to move off the browser UI thread
                Task.Factory.StartNew(message =>
                {
                    // TODO: Fix this. Photino should ideally tell us the URL that the message comes from so we
                    // know whether to trust it. Currently it's hardcoded to trust messages from any source, including
                    // if the webview is somehow navigated to an external URL.
                    var messageOriginUrl = new Uri(AppBaseUri);

                    MessageReceived(messageOriginUrl, (string)message!);
                }, message);
            };
        }

        public Stream? HandleWebRequest(string url, out string? contentType)
        {
            // It would be better if we were told whether or not this is a navigation request, but
            // since we're not, guess.
            var hasFileExtension = url.LastIndexOf('.') > url.LastIndexOf('/');

            if (url.StartsWith(AppBaseUri, StringComparison.Ordinal)
                && TryGetResponseContent(url, !hasFileExtension, out var statusCode, out var statusMessage, out var content, out var headers))
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
            _window.SendWebMessage(message);
        }
    }
}
