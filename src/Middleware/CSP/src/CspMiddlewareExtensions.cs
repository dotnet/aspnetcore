using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Csp
{
    public static class CspMiddlewareExtensions
    {
        public static IApplicationBuilder UseCsp(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CspMiddleware>();
        }
    }
}
