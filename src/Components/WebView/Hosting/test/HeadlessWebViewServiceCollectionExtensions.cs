using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    public static class HeadlessWebViewServiceCollectionExtensions
    {
        public static IServiceCollection AddHeadlessWebView(this IServiceCollection services)
        {
            services.AddBlazorWebView();
            services.TryAddSingleton(Dispatcher.CreateDefault());

            return services;
        }
    }
}
