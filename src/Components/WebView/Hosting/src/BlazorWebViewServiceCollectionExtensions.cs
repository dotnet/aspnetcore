using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView
{
    public static class BlazorWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazorWebView(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<IJSRuntime, WebViewJSRuntime>();
            services.AddScoped<INavigationInterception, WebViewNavigationInterception>();
            services.AddScoped<NavigationManager, WebViewNavigationManager>();

            return services;
        }
    }
}
