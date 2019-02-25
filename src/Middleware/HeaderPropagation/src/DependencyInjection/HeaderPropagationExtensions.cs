using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public static class HeaderPropagationExtensions
    {
        public static IServiceCollection AddHeaderPropagation(this IServiceCollection services, Action<HeaderPropagationOptions> configure)
        {
            services.TryAddSingleton<HeaderPropagationState>();
            services.Configure(configure);

            return services;
        }

        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder)
        {
            builder.Services.TryAddSingleton<HeaderPropagationState>();
            builder.Services.TryAddTransient<HeaderPropagationMessageHandler>();
 
            builder.AddHttpMessageHandler(services =>
            {
                var state = services.GetRequiredService<HeaderPropagationState>();
                return new HeaderPropagationMessageHandler(services.GetRequiredService<IOptions<HeaderPropagationOptions>>(), state);
            });

            return builder;
        }

        public static IApplicationBuilder UseHeaderPropagation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HeaderPropagationMiddleware>();
        }
    }
}
