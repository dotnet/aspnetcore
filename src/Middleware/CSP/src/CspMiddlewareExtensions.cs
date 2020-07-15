using System;
using Microsoft.AspNetCore.Builder;

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

            return app.UseMiddleware<CspMiddleware>(policyBuilder.Build());
        }
    }
}
