// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class TestWebViewManager : WebViewManager
    {
        private static Uri AppBaseUri = new Uri("app://testhost/");
        private List<string> _sentIpcMessages = new();

        public TestWebViewManager(IServiceProvider provider, IFileProvider fileProvider)
            : base(provider, Dispatcher.CreateDefault(), AppBaseUri, fileProvider, hostPageRelativePath: "index.html")
        {
        }

        public IReadOnlyList<string> SentIpcMessages => _sentIpcMessages;

        protected override void SendMessage(string message)
        {
            _sentIpcMessages.Add(message);
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            throw new NotImplementedException();
        }

        internal void ReceiveIpcMessage(IpcCommon.IncomingMessageType messageType, params object[] args)
        {
            // Same serialization convention as used by blazor.webview.js
            MessageReceived(new Uri(AppBaseUri, "/page"), IpcCommon.Serialize(messageType, args));
        }

        public void ReceiveAttachPageMessage()
        {
            ReceiveIpcMessage(IpcCommon.IncomingMessageType.AttachPage, "http://example/", "http://example/testStartUrl");
        }
    }
}
