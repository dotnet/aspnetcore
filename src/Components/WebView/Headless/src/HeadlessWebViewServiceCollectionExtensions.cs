using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    public static class HeadlessWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddHeadlessWebView(this IServiceCollection services)
        {
            services.AddBlazorWebView();
            services.AddScoped<WebViewHost, HeadlessWebViewHost>();

            return services;
        }
    }
}
