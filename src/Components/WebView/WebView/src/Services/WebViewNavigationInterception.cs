using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebView.Services
{
    internal class WebViewNavigationInterception : INavigationInterception
    {
        // On this platform, it's sufficient for the JS-side code to enable it unconditionally,
        // so there's no need to send a notification.
        public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
    }
}
