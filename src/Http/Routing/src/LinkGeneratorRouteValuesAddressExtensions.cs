// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Extension methods for using <see cref="LinkGenerator"/> with <see cref="RouteValuesAddress"/>.
/// </summary>
public static class LinkGeneratorRouteValuesAddressExtensions
{
    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">
    /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
    /// </param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    public static string? GetPathByRouteValues(
        this LinkGenerator generator,
        HttpContext httpContext,
        string? routeName,
        object? values,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(httpContext);

        var address = CreateAddress(httpContext, routeName, new(values));
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
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">
    /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
    /// </param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static string? GetPathByRouteValues(
        this LinkGenerator generator,
        HttpContext httpContext,
        string? routeName,
        RouteValueDictionary? values = default,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(httpContext);

        var address = CreateAddress(httpContext, routeName, values);
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
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    public static string? GetPathByRouteValues(
        this LinkGenerator generator,
        string? routeName,
        object? values,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var address = CreateAddress(httpContext: null, routeName, new(values));
        return generator.GetPathByAddress<RouteValuesAddress>(address, address.ExplicitValues, pathBase, fragment, options);
    }

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static string? GetPathByRouteValues(
        this LinkGenerator generator,
        string? routeName,
        RouteValueDictionary? values = default,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var address = CreateAddress(httpContext: null, routeName, values);
        return generator.GetPathByAddress<RouteValuesAddress>(address, address.ExplicitValues, pathBase, fragment, options);
    }

    /// <summary>
    /// Generates an absolute URI based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
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
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    public static string? GetUriByRouteValues(
        this LinkGenerator generator,
        HttpContext httpContext,
        string? routeName,
        object? values,
        string? scheme = default,
        HostString? host = default,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(httpContext);

        var address = CreateAddress(httpContext, routeName, new(values));
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
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static string? GetUriByRouteValues(
        this LinkGenerator generator,
        HttpContext httpContext,
        string? routeName,
        RouteValueDictionary? values = default,
        string? scheme = default,
        HostString? host = default,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(httpContext);

        var address = CreateAddress(httpContext, routeName, values);
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
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template. Optional.</param>
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
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    public static string? GetUriByRouteValues(
        this LinkGenerator generator,
        string? routeName,
        object? values,
        string scheme,
        HostString host,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var address = CreateAddress(httpContext: null, routeName, new(values));
        return generator.GetUriByAddress<RouteValuesAddress>(address, address.ExplicitValues, scheme, host, pathBase, fragment, options);
    }

    /// <summary>
    /// Generates an absolute URI based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator"/>.</param>
    /// <param name="routeName">The route name. Used to resolve endpoints. Optional.</param>
    /// <param name="values">The route values. Used to resolve endpoints and expand parameters in the route template.</param>
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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static string? GetUriByRouteValues(
        this LinkGenerator generator,
        string? routeName,
        RouteValueDictionary values,
        string scheme,
        HostString host,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var address = CreateAddress(httpContext: null, routeName, values);
        return generator.GetUriByAddress<RouteValuesAddress>(address, address.ExplicitValues, scheme, host, pathBase, fragment, options);
    }

    private static RouteValuesAddress CreateAddress(HttpContext? httpContext, string? routeName, RouteValueDictionary? values)
    {
        return new RouteValuesAddress()
        {
            AmbientValues = DefaultLinkGenerator.GetAmbientValues(httpContext),
            ExplicitValues = values ?? new(),
            RouteName = routeName,
        };
    }
}
