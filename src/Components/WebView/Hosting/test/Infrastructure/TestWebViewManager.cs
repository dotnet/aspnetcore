using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class TestWebViewManager : WebViewManager
    {
        private List<string> _sentIpcMessages = new();

        public TestWebViewManager(IServiceProvider provider)
            : base(provider, Dispatcher.CreateDefault())
        {
        }

        public IReadOnlyList<string> SentIpcMessages => _sentIpcMessages;

        protected override void SendMessage(string message)
        {
            _sentIpcMessages.Add(message);
        }

        public override void Navigate(string absoluteUrl)
        {
            throw new NotImplementedException();
        }

        public void ReceiveIpcMessage(string messageType, params object[] args)
        {
            // Same serialization convention as used by blazor.webview.js
            var serializedMessage = $"__bwv:{JsonSerializer.Serialize(new object[] { messageType }.Concat(args))}";
            MessageReceived(serializedMessage);
        }

        public void ReceiveInitializationMessage()
        {
            ReceiveIpcMessage("Initialize", "http://example/", "http://example/testStartUrl");
        }
    }
}
