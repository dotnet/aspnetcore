// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.WebView;

public class TestWebViewManager : WebViewManager
{
    private static readonly Uri AppBaseUri = new Uri("app://testhost/");
    private readonly List<string> _sentIpcMessages = new();

    public TestWebViewManager(IServiceProvider provider, IFileProvider fileProvider)
        : base(provider, Dispatcher.CreateDefault(), AppBaseUri, fileProvider, new(), hostPageRelativePath: "index.html")
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
