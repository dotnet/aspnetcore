// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains extension methods for using Razor Pages with <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class RazorPagesEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds endpoints for Razor Pages to the <see cref="IEndpointRouteBuilder"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with Razor Pages.</returns>
        public static IEndpointConventionBuilder MapRazorPages(this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            EnsureRazorPagesServices(routes);

            return GetOrCreateDataSource(routes);
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="page">The page name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToPage(IEndpointRouteBuilder, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToPage(IEndpointRouteBuilder, string)"/> registers an endpoint using the pattern
        /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToPage(IEndpointRouteBuilder, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the <c>path</c> route value
        /// will be available. 
        /// </para>
        /// </remarks>
        public static void MapFallbackToPage(this IEndpointRouteBuilder routes, string page)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(routes);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(routes);

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run. 
            routes.MapFallback(context => Task.CompletedTask).Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area: null));
            });
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="page">The action name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToPage(IEndpointRouteBuilder, string, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
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
        /// <see cref="MapFallbackToPage(IEndpointRouteBuilder, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the route values provided by matching 
        /// <paramref name="pattern"/> will be available. 
        /// </para>
        /// </remarks>
        public static void MapFallbackToPage(
            this IEndpointRouteBuilder routes,
            string pattern,
            string page)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(routes);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(routes);

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run. 
            routes.MapFallback(pattern, context => Task.CompletedTask).Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area: null));
            });
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="page">The action name.</param>
        /// <param name="area">The area name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToAreaPage(IEndpointRouteBuilder, string, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
        /// result in an HTTP 404.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaPage(IEndpointRouteBuilder, string, string)"/> registers an endpoint using the pattern
        /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
        /// </para>
        /// <para>
        /// <see cref="MapFallbackToAreaPage(IEndpointRouteBuilder, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the <c>path</c> route value
        /// will be available. 
        /// </para>
        /// </remarks>
        public static void MapFallbackToAreaPage(
            this IEndpointRouteBuilder routes,
            string page,
            string area)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(routes);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(routes);

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run. 
            routes.MapFallback(context => Task.CompletedTask).Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area));
            });
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="page">The action name.</param>
        /// <param name="area">The area name.</param>
        /// <remarks>
        /// <para>
        /// <see cref="MapFallbackToAreaPage(IEndpointRouteBuilder, string, string, string)"/> is intended to handle cases where URL path of
        /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
        /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
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
        /// <see cref="MapFallbackToAreaPage(IEndpointRouteBuilder, string, string, string)"/> does not re-execute routing, and will
        /// not generate route values based on routes defined elsewhere. When using this overload, the route values provided by matching 
        /// <paramref name="pattern"/> will be available.
        /// </para>
        /// </remarks>
        public static void MapFallbackToAreaPage(
            this IEndpointRouteBuilder routes,
            string pattern,
            string page,
            string area)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(routes);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(routes);

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run. 
            routes.MapFallback(pattern, context => Task.CompletedTask).Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area));
            });
        }

        private static DynamicPageMetadata CreateDynamicPageMetadata(string page, string area)
        {
            return new DynamicPageMetadata(new RouteValueDictionary()
            {
                { "page", page },
                { "area", area }
            });
        }

        private static void EnsureRazorPagesServices(IEndpointRouteBuilder routes)
        {
            var marker = routes.ServiceProvider.GetService<PageActionEndpointDataSource>();
            if (marker == null)
            {
                throw new InvalidOperationException(Mvc.Core.Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddMvc",
                    "ConfigureServices(...)"));
            }
        }

        private static PageActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder routes)
        {
            var dataSource = routes.DataSources.OfType<PageActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = routes.ServiceProvider.GetRequiredService<PageActionEndpointDataSource>();
                routes.DataSources.Add(dataSource);
            }

            return dataSource;
        }
    }
}
