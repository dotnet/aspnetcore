using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Csp
{
    public static class CspMiddlewareExtensions
    {
        public static IApplicationBuilder UseCsp(this IApplicationBuilder app, Action<ContentSecurityPolicyBuilder> configurePolicy)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var policyBuilder = new ContentSecurityPolicyBuilder();
            configurePolicy(policyBuilder);

            // TODO: Has local reporting
            if (policyBuilder.HasReporting())
            {
                var loggingConfig = policyBuilder.LoggingConfiguration();
                app.UseWhen(
                    context => context.Request.Path.StartsWithSegments(loggingConfig.ReportUri),
                    appBuilder => appBuilder.UseMiddleware<CspReportingMiddleware>(loggingConfig));
            }

            return app.UseMiddleware<CspMiddleware>(policyBuilder.Build());
        }

        public static IServiceCollection AddNonces(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<INonce, Nonce>();

            return services;
        }
    }
}
