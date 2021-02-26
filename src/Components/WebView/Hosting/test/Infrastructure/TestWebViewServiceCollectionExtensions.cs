using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.WebView
{
    public static class TestWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddTestBlazorWebView(this IServiceCollection services)
        {
            services.AddBlazorWebView();
            services.TryAddSingleton(Dispatcher.CreateDefault());

            return services;
        }
    }
}
