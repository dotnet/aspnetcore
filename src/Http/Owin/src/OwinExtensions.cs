// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;

namespace Microsoft.AspNetCore.Builder
{
    using AddMiddleware = Action<Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >>;
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using CreateMiddleware = Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >;

    public static class OwinExtensions
    {
        public static AddMiddleware UseOwin(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

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
                        var owinEnvFeature = httpContext.Features.Get<IOwinEnvironmentFeature>();
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

        public static IApplicationBuilder UseOwin(this IApplicationBuilder builder, Action<AddMiddleware> pipeline)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            pipeline(builder.UseOwin());
            return builder;
        }

        public static IApplicationBuilder UseBuilder(this AddMiddleware app)
        {
            return app.UseBuilder(serviceProvider: null);
        }

        public static IApplicationBuilder UseBuilder(this AddMiddleware app, IServiceProvider serviceProvider)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // Do not set ApplicationBuilder.ApplicationServices to null. May fail later due to missing services but
            // at least that results in a more useful Exception than a NRE.
            if (serviceProvider == null)
            {
                serviceProvider = new EmptyProvider();
            }

            // Adapt WebSockets by default.
            app(OwinWebSocketAcceptAdapter.AdaptWebSockets);
            var builder = new ApplicationBuilder(serviceProvider: serviceProvider);

            var middleware = CreateMiddlewareFactory(exit =>
            {
                builder.Use(ignored => exit);
                return builder.Build();
            }, builder.ApplicationServices);

            app(middleware);
            return builder;
        }

        private static CreateMiddleware CreateMiddlewareFactory(Func<RequestDelegate, RequestDelegate> middleware, IServiceProvider services)
        {
            return next =>
            {
                var app = middleware(httpContext =>
                {
                    return next(httpContext.Features.Get<IOwinEnvironmentFeature>().Environment);
                });

                return env =>
                {
                    // Use the existing HttpContext if there is one.
                    HttpContext context;
                    object obj;
                    if (env.TryGetValue(typeof(HttpContext).FullName, out obj))
                    {
                        context = (HttpContext)obj;
                        context.Features.Set<IOwinEnvironmentFeature>(new OwinEnvironmentFeature() { Environment = env });
                    }
                    else
                    {
                        context = new DefaultHttpContext(
                                    new FeatureCollection(
                                        new OwinFeatureCollection(env)));
                        context.RequestServices = services;
                    }

                    return app.Invoke(context);
                };
            };
        }

        public static AddMiddleware UseBuilder(this AddMiddleware app, Action<IApplicationBuilder> pipeline)
        {
            return app.UseBuilder(pipeline, serviceProvider: null);
        }

        public static AddMiddleware UseBuilder(this AddMiddleware app, Action<IApplicationBuilder> pipeline, IServiceProvider serviceProvider)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            var builder = app.UseBuilder(serviceProvider);
            pipeline(builder);
            return app;
        }

        private class EmptyProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}
