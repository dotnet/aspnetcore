using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Csp;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CspServiceCollectionExtensions
    {
        public static IServiceCollection AddCsp(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.TryAdd(ServiceDescriptor.Transient<ICspService, CspService>());
            services.TryAdd(ServiceDescriptor.Transient<IContentSecurityPolicyProvider, DefaultContentSecurityPolicyProvider>());

            return services;
        }

        public static IServiceCollection AddCsp(this IServiceCollection services, Action<CspOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddCsp();
            services.Configure(setupAction);

            return services;
        }
    }
}
