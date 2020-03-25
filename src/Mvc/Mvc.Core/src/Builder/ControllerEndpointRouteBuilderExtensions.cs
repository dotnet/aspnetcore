// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains extension methods for using Controllers with <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class ControllerEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> without specifying any routes.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="ControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions.</returns>
        public static ControllerActionEndpointConventionBuilder MapControllers(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureControllerServices(endpoints);

            return GetOrCreateDataSource(endpoints).DefaultBuilder;
        }

        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and adds the default route
        /// <c>{controller=Home}/{action=Index}/{id?}</c>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>
        /// An <see cref="ControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions for this route.
        /// </returns>
        public static ControllerActionEndpointConventionBuilder MapDefaultControllerRoute(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureControllerServices(endpoints);

            var dataSource = GetOrCreateDataSource(endpoints);
            return dataSource.AddRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}",
                defaults: null,
                constraints: null,
                dataTokens: null);
        }

        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="pattern"/>,
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        /// <returns>
        /// An <see cref="ControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions for this route.
        /// </returns>
        public static ControllerActionEndpointConventionBuilder MapControllerRoute(
            this IEndpointRouteBuilder endpoints,
            string name,
            string pattern,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureControllerServices(endpoints);

            var dataSource = GetOrCreateDataSource(endpoints);
            return dataSource.AddRoute(
                name,
                pattern,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        /// <summary>
        /// Adds endpoints for controller actions to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="areaName"/>, <paramref name="pattern"/>,
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="areaName">The area name.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        /// <returns>
        /// An <see cref="ControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions for this route.
        /// </returns>
        public static ControllerActionEndpointConventionBuilder MapAreaControllerRoute(
            this IEndpointRouteBuilder endpoints,
            string name,
            string areaName,
            string pattern,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (string.IsNullOrEmpty(areaName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(areaName));
            }

            var defaultsDictionary = new RouteValueDictionary(defaults);
            defaultsDictionary["area"] = defaultsDictionary["area"] ?? areaName;

            var constraintsDictionary = new RouteValueDictionary(constraints);
            constraintsDictionary["area"] = constraintsDictionary["area"] ?? new StringRouteConstraint(areaName);

            return endpoints.MapControllerRoute(name, pattern, defaultsDictionary, constraintsDictionary, dataTokens);
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a controller endpoint that
        /// matches <paramref name="action"/>, and <paramref name="controller"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="action">The action name.</param>
        /// <param name="controller">The controller name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string)"/> registers an endpoint using the pattern
        /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the <c>path</c> route value
        /// will be available.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string)"/> does not attempt to disambiguate between
        /// multiple actions that match the provided <paramref name="action"/> and <paramref name="controller"/>. If multiple
        /// actions match these values, the result is implementation defined.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToController(
            this IEndpointRouteBuilder endpoints,
            string action,
            string controller)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            EnsureControllerServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicControllerMetadata(action, controller, area: null));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a controller endpoint that
        /// matches <paramref name="action"/>, and <paramref name="controller"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The action name.</param>
        /// <param name="controller">The controller name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string, string)"/> is intended to handle cases
        /// where URL path of the request does not contain a file name, and no other endpoint has matched. This is convenient
        /// for routing requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route contraint
        /// to exclude requests for static files.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the route values provided by matching
        /// <paramref name="pattern"/> will be available.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToController(IEndpointRouteBuilder, string, string, string)"/> does not attempt to disambiguate between
        /// multiple actions that match the provided <paramref name="action"/> and <paramref name="controller"/>. If multiple
        /// actions match these values, the result is implementation defined.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToController(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            string action,
            string controller)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            EnsureControllerServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(pattern, context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicControllerMetadata(action, controller, area: null));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a controller endpoint that
        /// matches <paramref name="action"/>, <paramref name="controller"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="action">The action name.</param>
        /// <param name="controller">The controller name.</param>
        /// <param name="area">The area name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string)"/> registers an endpoint using the pattern
        /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the <c>path</c> route value
        /// will be available.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string)"/> does not attempt to disambiguate between
        /// multiple actions that match the provided <paramref name="action"/>, <paramref name="controller"/>, and <paramref name="area"/>. If multiple
        /// actions match these values, the result is implementation defined.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToAreaController(
            this IEndpointRouteBuilder endpoints,
            string action,
            string controller,
            string area)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            EnsureControllerServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicControllerMetadata(action, controller, area));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a controller endpoint that
        /// matches <paramref name="action"/>, <paramref name="controller"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The action name.</param>
        /// <param name="controller">The controller name.</param>
        /// <param name="area">The area name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string, string)"/> is intended to handle
        /// cases where URL path of the request does not contain a file name, and no other endpoint has matched. This is
        /// convenient for routing requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route contraint
        /// to exclude requests for static files.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the route values provided by matching
        /// <paramref name="pattern"/> will be available.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaController(IEndpointRouteBuilder, string, string, string, string)"/> does not attempt to disambiguate between
        /// multiple actions that match the provided <paramref name="action"/>, <paramref name="controller"/>, and <paramref name="area"/>. If multiple
        /// actions match these values, the result is implementation defined.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToAreaController(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            string action,
            string controller,
            string area)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            EnsureControllerServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(pattern, context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicControllerMetadata(action, controller, area));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will
        /// attempt to select a controller action using the route values produced by <typeparamref name="TTransformer"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <typeparam name="TTransformer">The type of a <see cref="DynamicRouteValueTransformer"/>.</typeparam>
        /// <remarks>
        /// <para>
        /// This method allows the registration of a <see cref="RouteEndpoint"/> and <see cref="DynamicRouteValueTransformer"/>
        /// that combine to dynamically select a controller action using custom logic.
        /// </para>
        /// <para>
        /// The instance of <typeparamref name="TTransformer"/> will be retrieved from the dependency injection container.
        /// Register <typeparamref name="TTransformer"/> with the desired service lifetime in <c>ConfigureServices</c>.
        /// </para>
        /// </remarks>
        public static void MapDynamicControllerRoute<TTransformer>(this IEndpointRouteBuilder endpoints, string pattern)
            where TTransformer : DynamicRouteValueTransformer
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureControllerServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            endpoints.Map(
                pattern, 
                context =>
                {
                    throw new InvalidOperationException("This endpoint is not expected to be executed directly.");
                })
                .Add(b =>
                {
                    b.Metadata.Add(new DynamicControllerRouteValueTransformerMetadata(typeof(TTransformer)));
                });
        }

        private static DynamicControllerMetadata CreateDynamicControllerMetadata(string action, string controller, string area)
        {
            return new DynamicControllerMetadata(new RouteValueDictionary()
            {
                { "action", action },
                { "controller", controller },
                { "area", area }
            });
        }

        private static void EnsureControllerServices(IEndpointRouteBuilder endpoints)
        {
            var marker = endpoints.ServiceProvider.GetService<MvcMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddControllers",
                    "ConfigureServices(...)"));
            }
        }

        private static ControllerActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
        {
            var dataSource = endpoints.DataSources.OfType<ControllerActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = endpoints.ServiceProvider.GetRequiredService<ControllerActionEndpointDataSource>();
                endpoints.DataSources.Add(dataSource);
            }

            return dataSource;
        }
    }
}
