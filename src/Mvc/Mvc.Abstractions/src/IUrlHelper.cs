// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Defines the contract for the helper to build URLs for ASP.NET MVC within an application.
/// </summary>
public interface IUrlHelper
{
    /// <summary>
    /// Gets the <see cref="ActionContext"/> for the current request.
    /// </summary>
    ActionContext ActionContext { get; }

    /// <summary>
    /// Generates a URL with an absolute path for an action method, which contains the action
    /// name, controller name, route values, protocol to use, host name, and fragment specified by
    /// <see cref="UrlActionContext"/>. Generates an absolute URL if <see cref="UrlActionContext.Protocol"/> and
    /// <see cref="UrlActionContext.Host"/> are non-<c>null</c>. See the remarks section for important security information.
    /// </summary>
    /// <param name="actionContext">The context object for the generated URLs for an action method.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <see cref="UrlActionContext.Host" /> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    string? Action(UrlActionContext actionContext);

    /// <summary>
    /// Converts a virtual (relative, starting with ~/) path to an application absolute path.
    /// </summary>
    /// <remarks>
    /// If the specified content path does not start with the tilde (~) character,
    /// this method returns <paramref name="contentPath"/> unchanged.
    /// </remarks>
    /// <param name="contentPath">The virtual path of the content.</param>
    /// <returns>The application absolute path.</returns>
    [return: NotNullIfNotNull("contentPath")]
    string? Content(string? contentPath);

    /// <summary>
    /// Returns a value that indicates whether the URL is local. A URL is considered local if it does not have a
    /// host / authority part and it has an absolute path. URLs using virtual paths ('~/') are also local.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns><c>true</c> if the URL is local; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>
    /// For example, the following URLs are considered local:
    /// <code>
    /// /Views/Default/Index.html
    /// ~/Index.html
    /// </code>
    /// </para>
    /// <para>
    /// The following URLs are non-local:
    /// <code>
    /// ../Index.html
    /// http://www.contoso.com/
    /// http://localhost/Index.html
    /// </code>
    /// </para>
    /// </example>
    bool IsLocalUrl([NotNullWhen(true)][StringSyntax(StringSyntaxAttribute.Uri)] string? url);

    /// <summary>
    /// Generates a URL with an absolute path, which contains the route name, route values, protocol to use, host
    /// name, and fragment specified by <see cref="UrlRouteContext"/>. Generates an absolute URL if
    /// <see cref="UrlActionContext.Protocol"/> and <see cref="UrlActionContext.Host"/> are non-<c>null</c>.
    /// See the remarks section for important security information.
    /// </summary>
    /// <param name="routeContext">The context object for the generated URLs for a route.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <see cref="UrlRouteContext.Host" /> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    string? RouteUrl(UrlRouteContext routeContext);

    /// <summary>
    /// Generates an absolute URL for the specified <paramref name="routeName"/> and route
    /// <paramref name="values"/>, which contains the protocol (such as "http" or "https") and host name from the
    /// current request. See the remarks section for important security information.
    /// </summary>
    /// <param name="routeName">The name of the route that is used to generate URL.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <returns>The generated absolute URL.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the value of <see cref="HttpRequest.Host"/> to populate the host section of the generated URI.
    /// Relying on the value of the current request can allow untrusted input to influence the resulting URI unless
    /// the <c>Host</c> header has been validated. See the deployment documentation for instructions on how to properly
    /// validate the <c>Host</c> header in your deployment environment.
    /// </para>
    /// </remarks>
    string? Link(string? routeName, object? values);
}
