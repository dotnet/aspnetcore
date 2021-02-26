using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    public class HeadlessWebViewManager : WebViewManager
    {
        public HeadlessWebViewManager(IServiceProvider provider) : base(provider)
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
    }
}
