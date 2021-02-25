namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    internal class WebViewNavigationManager : NavigationManager
    {
        private readonly WebViewHost _host;

        public WebViewNavigationManager(WebViewHost host)
        {
            _host = host;
            _host.LocationUpdated += LocationUpdated;
            Initialize(_host.BaseUrl, _host.CurrentUrl);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            _host.Navigate(uri, forceLoad);
        }

        public void LocationUpdated(string newUrl, bool intercepted)
        {
            Uri = newUrl;
            NotifyLocationChanged(intercepted);
        }
    }
}
