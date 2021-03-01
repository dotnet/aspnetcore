namespace Microsoft.AspNetCore.Components.WebView
{
    internal class WebViewNavigationManager : NavigationManager
    {
        private IpcSender _ipcSender;

        public void AttachToWebView(IpcSender ipcSender, string baseUrl, string initialUrl)
        {
            _ipcSender = ipcSender;
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
