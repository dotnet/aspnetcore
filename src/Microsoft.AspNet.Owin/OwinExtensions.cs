using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Owin;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using CreateMiddleware = Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >;
    using AddMiddleware = Action<Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >>;

    public static class OwinExtensions
    {
        public static AddMiddleware UseOwin(this IBuilder builder)
        {
            return middleware =>
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
                builder.Use(middleware1);
            };
        }

        public static IBuilder UseOwin(this IBuilder builder, Action<AddMiddleware> pipeline)
        {
            pipeline(builder.UseOwin());
            return builder;
        }

        public static IBuilder UseBuilder(this AddMiddleware app)
        {
            var builder = new Builder(serviceProvider: null);

            CreateMiddleware middleware = CreateMiddlewareFactory(exit =>
            {
                builder.Use(ignored => exit);
                return builder.Build();
            });

            app(middleware);
            return builder;
        }

        private static CreateMiddleware CreateMiddlewareFactory(Func<RequestDelegate, RequestDelegate> middleware)
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

        public static AddMiddleware UseBuilder(this AddMiddleware app, Action<IBuilder> pipeline)
        {
            var builder = app.UseBuilder();
            pipeline(builder);
            return app;
        }
    }
}
