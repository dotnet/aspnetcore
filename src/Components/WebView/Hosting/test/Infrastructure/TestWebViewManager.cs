using System;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class TestWebViewManager : WebViewManager
    {
        public TestWebViewManager(IServiceProvider provider)
            : base(provider, Dispatcher.CreateDefault())
        {
        }

        public event Action<string> OnSentMessage;

        protected override void SendMessage(string message)
        {
            OnSentMessage?.Invoke(message);
        }

        public void IncomingMessage(string message)
        {
            MessageReceived(message);
        }

        internal void SetUrls(string baseUrl, string initialUrl)
        {
            BaseUrl = baseUrl;
            StartUrl = initialUrl;
        }
    }
}
