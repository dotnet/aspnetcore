namespace Microsoft.AspNetCore.Components.WebView
{
    internal class WebViewNavigationManager : NavigationManager
    {
        private readonly IpcSender _ipcSender;

        public WebViewNavigationManager(IpcSender ipcSender)
        {
            _ipcSender = ipcSender;
        }

        public void Init(string baseUrl, string initialUrl)
        {
            Initialize(baseUrl, initialUrl);
        }

        public void LocationUpdated(string newUrl, bool intercepted)
        {
            Uri = newUrl;
            NotifyLocationChanged(intercepted);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            _ipcSender.Navigate(uri, forceLoad);
        }
    }
}
