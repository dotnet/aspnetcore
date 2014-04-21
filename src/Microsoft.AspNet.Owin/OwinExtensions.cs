using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Owin;

namespace Microsoft.AspNet
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class OwinExtensions
    {
        public static IBuilder UseOwinMiddleware(this IBuilder builder, Func<AppFunc, AppFunc> middleware)
        {
            Func<RequestDelegate, RequestDelegate> middleware1 = next1 =>
            {
                AppFunc exitMiddlware = env =>
                {
                    return next1((HttpContext)env[typeof(HttpContext).FullName]);
                };
                var app = middleware(exitMiddlware);
                return httpContext =>
                {
                    return app.Invoke(new OwinEnvironment(httpContext));
                };
            };
            return builder.Use(middleware1);
        }
    }
}
