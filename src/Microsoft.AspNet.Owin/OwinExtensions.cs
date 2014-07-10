// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Owin;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet.Builder
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
            AddMiddleware add = middleware =>
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
                        // Use the existing OWIN env if there is one.
                        IDictionary<string, object> env;
                        var owinEnvFeature = httpContext.GetFeature<IOwinEnvironmentFeature>();
                        if (owinEnvFeature != null)
                        {
                            env = owinEnvFeature.Environment;
                            env[typeof(HttpContext).FullName] = httpContext;
                        }
                        else
                        {
                            env = new OwinEnvironment(httpContext);
                        }
                        return app.Invoke(env);
                    };
                };
                builder.Use(middleware1);
            };
            // Adapt WebSockets by default.
            add(WebSocketAcceptAdapter.AdaptWebSockets);
            return add;
        }

        public static IBuilder UseOwin(this IBuilder builder, Action<AddMiddleware> pipeline)
        {
            pipeline(builder.UseOwin());
            return builder;
        }

        public static IBuilder UseBuilder(this AddMiddleware app)
        {
            // Adapt WebSockets by default.
            app(OwinWebSocketAcceptAdapter.AdaptWebSockets);
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
                    return next(httpContext.GetFeature<IOwinEnvironmentFeature>().Environment);
                });

                return env =>
                {
                    // Use the existing HttpContext if there is one.
                    HttpContext context;
                    object obj;
                    if (env.TryGetValue(typeof(HttpContext).FullName, out obj))
                    {
                        context = (HttpContext)obj;
                        context.SetFeature<IOwinEnvironmentFeature>(new OwinEnvironmentFeature() { Environment = env });
                    }
                    else
                    {
                        context = new DefaultHttpContext(
                                    new FeatureCollection(
                                        new OwinFeatureCollection(env)));
                    }

                    return app.Invoke(context);
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
