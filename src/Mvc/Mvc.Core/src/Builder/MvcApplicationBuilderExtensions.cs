// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to add MVC to the request execution pipeline.
/// </summary>
public static class MvcApplicationBuilderExtensions
{
    /// <summary>
    /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <remarks>This method only supports attribute routing. To add conventional routes use
    /// <see cref="UseMvc(IApplicationBuilder, Action{IRouteBuilder})"/>.</remarks>
    public static IApplicationBuilder UseMvc(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMvc(routes =>
        {
        });
    }

    /// <summary>
    /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline
    /// with a default route named 'default' and the following template:
    /// '{controller=Home}/{action=Index}/{id?}'.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseMvcWithDefaultRoute(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMvc(routes =>
        {
            routes.MapRoute(
                name: "default",
                template: "{controller=Home}/{action=Index}/{id?}");
        });
    }

    /// <summary>
    /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="configureRoutes">A callback to configure MVC routes.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseMvc(
        this IApplicationBuilder app,
        Action<IRouteBuilder> configureRoutes)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configureRoutes);

        VerifyMvcIsRegistered(app);

        var options = app.ApplicationServices.GetRequiredService<IOptions<MvcOptions>>();

        if (options.Value.EnableEndpointRouting)
        {
            var message =
                "Endpoint Routing does not support 'IApplicationBuilder.UseMvc(...)'. To use " +
                "'IApplicationBuilder.UseMvc' set 'MvcOptions.EnableEndpointRouting = false' inside " +
                "'ConfigureServices(...).";
            throw new InvalidOperationException(message);
        }

        var routes = new RouteBuilder(app)
        {
            DefaultHandler = app.ApplicationServices.GetRequiredService<MvcRouteHandler>(),
        };

        configureRoutes(routes);

        routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(app.ApplicationServices));

        return app.UseRouter(routes.Build());
    }

    private sealed class EndpointRouteBuilder : IRouteBuilder
    {
        public EndpointRouteBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder;
            Routes = new List<IRouter>();
            DefaultHandler = NullRouter.Instance;
        }

        public IApplicationBuilder ApplicationBuilder { get; }

        public IRouter? DefaultHandler { get; set; }

        public IServiceProvider ServiceProvider
        {
            get { return ApplicationBuilder.ApplicationServices; }
        }

        public IList<IRouter> Routes { get; }

        public IRouter Build()
        {
            throw new NotSupportedException();
        }
    }

    private static void VerifyMvcIsRegistered(IApplicationBuilder app)
    {
        // Verify if AddMvc was done before calling UseMvc
        // We use the MvcMarkerService to make sure if all the services were added.
        if (app.ApplicationServices.GetService(typeof(MvcMarkerService)) == null)
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                "AddMvc",
                "ConfigureServices(...)"));
        }
    }
}
