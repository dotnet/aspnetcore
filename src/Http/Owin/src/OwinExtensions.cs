// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;

namespace Microsoft.AspNetCore.Builder;

using AddMiddleware = Action<Func<
      Func<IDictionary<string, object>, Task>,
      Func<IDictionary<string, object>, Task>
    >>;
using AppFunc = Func<IDictionary<string, object>, Task>;
using CreateMiddleware = Func<
      Func<IDictionary<string, object>, Task>,
      Func<IDictionary<string, object>, Task>
    >;

/// <summary>
/// Extension methods to add OWIN to an HTTP application pipeline.
/// </summary>
public static class OwinExtensions
{
    /// <summary>
    /// Adds an OWIN pipeline to the specified <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the pipeline to.</param>
    /// <returns>An action used to create the OWIN pipeline.</returns>
    public static AddMiddleware UseOwin(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddMiddleware add = middleware =>
        {
            Func<RequestDelegate, RequestDelegate> middleware1 = next1 =>
            {
                AppFunc exitMiddleware = env =>
                {
                    return next1((HttpContext)env[typeof(HttpContext).FullName]);
                };
                var app = middleware(exitMiddleware);
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

    /// <summary>
    /// Adds OWIN middleware pipeline to the specified <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <param name="pipeline">A delegate which can specify the OWIN pipeline.</param>
    /// <returns>The original <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseOwin(this IApplicationBuilder builder, Action<AddMiddleware> pipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pipeline);

        pipeline(builder.UseOwin());
        return builder;
    }

    /// <summary>
    /// Creates an <see cref="IApplicationBuilder"/> for an OWIN pipeline.
    /// </summary>
    /// <param name="app">The OWIN pipeline.</param>
    /// <returns>An <see cref="IApplicationBuilder"/></returns>
    public static IApplicationBuilder UseBuilder(this AddMiddleware app)
    {
        return app.UseBuilder(serviceProvider: null);
    }

    /// <summary>
    /// Creates an <see cref="IApplicationBuilder"/> for an OWIN pipeline.
    /// </summary>
    /// <param name="app">The OWIN pipeline.</param>
    /// <param name="serviceProvider">A service provider for <see cref="IApplicationBuilder.ApplicationServices"/>.</param>
    /// <returns>An <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseBuilder(this AddMiddleware app, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(app);

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

    /// <summary>
    /// Creates an <see cref="IApplicationBuilder"/> for an OWIN pipeline.
    /// </summary>
    /// <param name="app">The OWIN pipeline.</param>
    /// <param name="pipeline">A delegate used to configure a middleware pipeline.</param>
    /// <returns>An <see cref="IApplicationBuilder"/>.</returns>
    public static AddMiddleware UseBuilder(this AddMiddleware app, Action<IApplicationBuilder> pipeline)
    {
        return app.UseBuilder(pipeline, serviceProvider: null);
    }

    /// <summary>
    /// Creates an <see cref="IApplicationBuilder"/> for an OWIN pipeline.
    /// </summary>
    /// <param name="app">The OWIN pipeline.</param>
    /// <param name="pipeline">A delegate used to configure a middleware pipeline.</param>
    /// <param name="serviceProvider">A service provider for <see cref="IApplicationBuilder.ApplicationServices"/>.</param>
    /// <returns>An <see cref="IApplicationBuilder"/>.</returns>
    public static AddMiddleware UseBuilder(this AddMiddleware app, Action<IApplicationBuilder> pipeline, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(pipeline);

        var builder = app.UseBuilder(serviceProvider);
        pipeline(builder);
        return app;
    }

    private sealed class EmptyProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return null;
        }
    }
}
