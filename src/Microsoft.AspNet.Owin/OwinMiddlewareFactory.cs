using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet.Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class OwinMiddlewareFactory
    {
        public static Func<AppFunc, AppFunc> Create(Action<IBuilder> configuration)
        {
            return Create(services: null, configuration: configuration);
        }

        public static Func<AppFunc, AppFunc> Create(IServiceProvider services, Action<IBuilder> configuration)
        {
            var builder = new Builder(services);
            configuration(builder);

            return Create(exit =>
            {
                builder.Use(ignored => exit);
                return builder.Build();
            });
        }

        public static Func<AppFunc, AppFunc> Create(Func<RequestDelegate, RequestDelegate> middleware)
        {
            return next =>
            {
                var app = middleware(httpContext =>
                {
                    return next(httpContext.GetFeature<ICanHasOwinEnvironment>().Environment);
                });

                return env =>
                {
                    return app.Invoke(
                        new DefaultHttpContext(
                            new FeatureCollection(
                                    new OwinFeatureCollection(env))));
                };
            };
        }
    }
}
