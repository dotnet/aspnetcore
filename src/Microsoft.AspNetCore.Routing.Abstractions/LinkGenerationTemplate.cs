// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract to generate a URL from a template.
    /// </summary>
    /// <remarks>
    /// A <see cref="LinkGenerationTemplate"/> can be created from <see cref="LinkGenerator.GetTemplateByAddress{TAddress}(TAddress, LinkGenerationTemplateOptions)"/>
    /// by supplying an address value which has matching endpoints. The returned <see cref="LinkGenerationTemplate"/>
    /// will be bound to the endpoints matching the address that was originally provided.
    /// </remarks>
    public abstract class LinkGenerationTemplate
    {
        /// <summary>
        /// Generates a URI with an absolute path based on the provided values.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
        /// <param name="pathBase">
        /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
        /// </param>
        /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
        public abstract string GetPath(
            HttpContext httpContext,
            object values,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default);

        /// <summary>
        /// Generates a URI with an absolute path based on the provided values.
        /// </summary>
        /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
        /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
        /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
        public abstract string GetPath(
            object values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default);

        /// <summary>
        /// Generates an absolute URI based on the provided values.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
        /// <param name="scheme">
        /// The URI scheme, applied to the resulting URI. Optional. If not provided, the value of <see cref="HttpRequest.Scheme"/> will be used.
        /// </param>
        /// <param name="host">
        /// The URI host/authority, applied to the resulting URI. Optional. If not provided, the value <see cref="HttpRequest.Host"/> will be used.
        /// See the remarks section for details about the security implications of the <paramref name="host"/>.
        /// </param>
        /// <param name="pathBase">
        /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
        /// </param>
        /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
        /// <remarks>
        /// <para>
        /// The value of <paramref name="host" /> should be a trusted value. Relying on the value of the current request
        /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
        /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
        /// your deployment environment.
        /// </para>
        /// </remarks>
        public abstract string GetUri(
            HttpContext httpContext,
            object values,
            string scheme = default,
            HostString? host = default,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default);

        /// <summary>
        /// Generates an absolute URI based on the provided values.
        /// </summary>
        /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
        /// <param name="scheme">The URI scheme, applied to the resulting URI.</param>
        /// <param name="host">
        /// The URI host/authority, applied to the resulting URI.
        /// See the remarks section for details about the security implications of the <paramref name="host"/>.
        /// </param>
        /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
        /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
        /// <param name="options">
        /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
        /// names from <c>RouteOptions</c>.
        /// </param>
        /// <returns>An absolute URI, or <c>null</c>.</returns>
        /// <remarks>
        /// <para>
        /// The value of <paramref name="host" /> should be a trusted value. Relying on the value of the current request
        /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
        /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
        /// your deployment environment.
        /// </para>
        /// </remarks>
        public abstract string GetUri(
            object values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default);
    }
}