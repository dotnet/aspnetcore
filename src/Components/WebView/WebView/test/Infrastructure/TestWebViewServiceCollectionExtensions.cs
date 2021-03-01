using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView
{
    public static class TestWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddTestBlazorWebView(this IServiceCollection services)
        {
            services.AddBlazorWebView();
            return services;
        }
    }
}
