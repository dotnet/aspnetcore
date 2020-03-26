// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Extension methods for using <see cref="LinkGenerator"/> to generate links to Razor Pages.
    /// </summary>
    public static class PageLinkGeneratorExtensions
    {
        /// <summary>
        /// Generates a URI with an absolute path based on the provided values.
        /// </summary>
        /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="page">
        /// The page name. Used to resolve endpoints. Optional. If <c>null</c> is provided, the current page route value
        /// will be used.
        /// </param>
        /// <param name="handler">
        /// The page handler name. Used to resolve endpoints. Optional.
        /// </param>
        /// <param name="values">The route values. Optional. Used to resolve endpoints and expand parameters in the route template.</param>
        /// <param name="pathBase">
        /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
        /// </param>
        /// <param name="fragment">A URI fragment. Optional. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A URI with an absolute path, or <c>null</c> if a URI cannot be created.</returns>
        public static string GetPathByPage(
            this LinkGenerator generator,
            HttpContext httpContext,
            string page = default,
            string handler = default,
            object values = default,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var address = CreateAddress(httpContext, page, handler, values);
            return generator.GetPathByAddress<RouteValuesAddress>(
                httpContext,
                address,
                address.ExplicitValues,
                address.AmbientValues,
                pathBase,
                fragment,
                options);
        }

        /// <summary>
        /// Generates a URI with an absolute path based on the provided values.
        /// </summary>
        /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
        /// <param name="page">
        /// The page name. Used to resolve endpoints.
        /// </param>
        /// <param name="handler">
        /// The page handler name. Used to resolve endpoints. Optional.
        /// </param>
        /// <param name="values">The route values. Optional. Used to resolve endpoints and expand parameters in the route template.</param>
        /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
        /// <param name="fragment">A URI fragment. Optional. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A URI with an absolute path, or <c>null</c> if a URI cannot be created.</returns>
        public static string GetPathByPage(
            this LinkGenerator generator,
            string page,
            string handler = default,
            object values = default,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            var address = CreateAddress(httpContext: null, page, handler, values);
            return generator.GetPathByAddress(address, address.ExplicitValues, pathBase, fragment, options);
        }

        /// <summary>
        /// Generates an absolute URI based on the provided values.
        /// </summary>
        /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="page">
        /// The page name. Used to resolve endpoints. Optional. If <c>null</c> is provided, the current page route value
        /// will be used.
        /// </param>
        /// <param name="handler">
        /// The page handler name. Used to resolve endpoints. Optional.
        /// </param>
        /// <param name="values">The route values. Optional. Used to resolve endpoints and expand parameters in the route template.</param>
        /// <param name="scheme">
        /// The URI scheme, applied to the resulting URI. Optional. If not provided, the value of <see cref="HttpRequest.Scheme"/> will be used.
        /// </param>
        /// <param name="host">
        /// The URI host/authority, applied to the resulting URI. Optional. If not provided, the value <see cref="HttpRequest.Host"/> will be used.
        /// </param>
        /// <param name="pathBase">
        /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
        /// </param>
        /// <param name="fragment">A URI fragment. Optional. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A absolute URI, or <c>null</c> if a URI cannot be created.</returns>
        /// <remarks>
        /// <para>
        /// The value of <paramref name="host" /> should be a trusted value. Relying on the value of the current request
        /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
        /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
        /// your deployment environment.
        /// </para>
        /// </remarks>
        public static string GetUriByPage(
            this LinkGenerator generator,
            HttpContext httpContext,
            string page = default,
            string handler = default,
            object values = default,
            string scheme = default,
            HostString? host = default,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var address = CreateAddress(httpContext, page, handler, values);
            return generator.GetUriByAddress<RouteValuesAddress>(
                httpContext,
                address,
                address.ExplicitValues,
                address.AmbientValues,
                scheme,
                host,
                pathBase,
                fragment,
                options);
        }

        /// <summary>
        /// Generates an absolute URI based on the provided values.
        /// </summary>
        /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
        /// <param name="page">The page name. Used to resolve endpoints.</param>
        /// <param name="handler">The page handler name. May be null.</param>
        /// <param name="values">The route values. May be null. Used to resolve endpoints and expand parameters in the route template.</param>
        /// <param name="scheme">The URI scheme, applied to the resulting URI.</param>
        /// <param name="host">The URI host/authority, applied to the resulting URI.</param>
        /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
        /// <param name="fragment">A URI fragment. Optional. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A absolute URI, or <c>null</c> if a URI cannot be created.</returns>
        /// <remarks>
        /// <para>
        /// The value of <paramref name="host" /> should be a trusted value. Relying on the value of the current request
        /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
        /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
        /// your deployment environment.
        /// </para>
        /// </remarks>
        public static string GetUriByPage(
            this LinkGenerator generator,
            string page,
            string handler,
            object values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            var address = CreateAddress(httpContext: null, page, handler, values);
            return generator.GetUriByAddress<RouteValuesAddress>(address, address.ExplicitValues, scheme, host, pathBase, fragment, options);
        }

        private static RouteValuesAddress CreateAddress(HttpContext httpContext, string page, string handler, object values)
        {
            var explicitValues = new RouteValueDictionary(values);
            var ambientValues = GetAmbientValues(httpContext);

            UrlHelperBase.NormalizeRouteValuesForPage(context: null, page, handler, explicitValues, ambientValues);

            return new RouteValuesAddress()
            {
                AmbientValues = ambientValues,
                ExplicitValues = explicitValues
            };
        }

        private static RouteValueDictionary GetAmbientValues(HttpContext httpContext)
        {
            return httpContext?.Features.Get<IRouteValuesFeature>()?.RouteValues;
        }
    }
}
