// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
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
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>An <see cref="PageActionEndpointConventionBuilder"/> for endpoints associated with Razor Pages.</returns>
        public static PageActionEndpointConventionBuilder MapRazorPages(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureRazorPagesServices(endpoints);

            return GetOrCreateDataSource(endpoints).DefaultBuilder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
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
        public static IEndpointConventionBuilder MapFallbackToPage(this IEndpointRouteBuilder endpoints, string page)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area: null));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
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
        public static IEndpointConventionBuilder MapFallbackToPage(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            string page)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
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

            EnsureRazorPagesServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(pattern, context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area: null));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
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
        public static IEndpointConventionBuilder MapFallbackToAreaPage(
            this IEndpointRouteBuilder endpoints,
            string page,
            string area)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority. The request will be routed to a page endpoint that
        /// matches <paramref name="page"/>, and <paramref name="area"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
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
        public static IEndpointConventionBuilder MapFallbackToAreaPage(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            string page,
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

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            PageConventionCollection.EnsureValidPageName(page, nameof(page));

            EnsureRazorPagesServices(endpoints);

            // Called for side-effect to make sure that the data source is registered.
            GetOrCreateDataSource(endpoints).CreateInertEndpoints = true;

            // Maps a fallback endpoint with an empty delegate. This is OK because
            // we don't expect the delegate to run.
            var builder = endpoints.MapFallback(pattern, context => Task.CompletedTask);
            builder.Add(b =>
            {
                // MVC registers a policy that looks for this metadata.
                b.Metadata.Add(CreateDynamicPageMetadata(page, area));
            });
            return builder;
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will
        /// attempt to select a page using the route values produced by <typeparamref name="TTransformer"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <typeparam name="TTransformer">The type of a <see cref="DynamicRouteValueTransformer"/>.</typeparam>
        /// <remarks>
        /// <para>
        /// This method allows the registration of a <see cref="RouteEndpoint"/> and <see cref="DynamicRouteValueTransformer"/>
        /// that combine to dynamically select a page using custom logic.
        /// </para>
        /// <para>
        /// The instance of <typeparamref name="TTransformer"/> will be retrieved from the dependency injection container.
        /// Register <typeparamref name="TTransformer"/> with the desired service lifetime in <c>ConfigureServices</c>.
        /// </para>
        /// </remarks>
        public static void MapDynamicPageRoute<TTransformer>(this IEndpointRouteBuilder endpoints, string pattern)
            where TTransformer : DynamicRouteValueTransformer
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            EnsureRazorPagesServices(endpoints);

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
                    b.Metadata.Add(new DynamicPageRouteValueTransformerMetadata(typeof(TTransformer)));
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

        private static void EnsureRazorPagesServices(IEndpointRouteBuilder endpoints)
        {
            var marker = endpoints.ServiceProvider.GetService<PageActionEndpointDataSource>();
            if (marker == null)
            {
                throw new InvalidOperationException(Mvc.Core.Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddRazorPages",
                    "ConfigureServices(...)"));
            }
        }

        private static PageActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
        {
            var dataSource = endpoints.DataSources.OfType<PageActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = endpoints.ServiceProvider.GetRequiredService<PageActionEndpointDataSource>();
                endpoints.DataSources.Add(dataSource);
            }

            return dataSource;
        }
    }
}
