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

            if (policyBuilder.HasLocalReporting())
            {
                var loggerFactory = app.ApplicationServices.GetService<ICspReportLoggerFactory>();
                var reportLogger = policyBuilder.ReportLogger(loggerFactory);
                app.UseWhen(
                    context => context.Request.Path.StartsWithSegments(reportLogger.ReportUri),
                    appBuilder => appBuilder.UseMiddleware<CspReportingMiddleware>(reportLogger));
            }

            return app.UseMiddleware<CspMiddleware>(policyBuilder.Build());
        }

        public static IServiceCollection AddCsp(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<INonce, Nonce>();
            services.AddSingleton<ICspReportLoggerFactory, CspReportLoggerFactory>();

            return services;
        }
    }
}
