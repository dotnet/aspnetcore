
using System;
using Microsoft.AspNetCore.RequestLimiter;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RequestLimiterServiceCollectionExtensions
    {
        // TODO: Does it make sense to have an overload where options are not configured?
        public static IServiceCollection AddRequestLimiter(this IServiceCollection services, Action<RequestLimiterOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);
            return services;
        }
    }
}
