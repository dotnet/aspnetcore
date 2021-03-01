using System;
using System.Linq;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class TestWebViewManager : WebViewManager
    {
        public TestWebViewManager(IServiceProvider provider)
            : base(provider, Dispatcher.CreateDefault())
        {
        }

        protected override void SendMessage(string message)
        {
            throw new NotImplementedException();
        }

        public override void Navigate(string absoluteUrl)
        {
            throw new NotImplementedException();
        }

        public void IncomingMessage(string messageType, params object[] args)
        {
            // Same serialization convention as used by blazor.webview.js
            var serializedMessage = $"__bwv:{JsonSerializer.Serialize(new object[] { messageType }.Concat(args))}";
            MessageReceived(serializedMessage);
        }
    }
}
