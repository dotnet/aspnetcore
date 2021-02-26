using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class TestWebViewManager : WebViewManager
    {
        public TestWebViewManager(IServiceProvider provider) : base(provider)
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
