// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Defines the contract for the helper to build URLs for ASP.NET MVC within an application.
    /// </summary>
    public interface IUrlHelper
    {
        /// <summary>
        /// Generates a fully qualified or absolute URL specified by <see cref="Mvc.UrlActionContext"/> for an action
        /// method, which contains action name, controller name, route values, protocol to use, host name, and fragment.
        /// </summary>
        /// <param name="actionContext">The context object for the generated URLs for an action method.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        string Action([NotNull] UrlActionContext actionContext);

        /// <summary>
        /// Converts a virtual (relative) path to an application absolute path.
        /// </summary>
        /// <remarks>
        /// If the specified content path does not start with the tilde (~) character,
        /// this method returns <paramref name="contentPath"/> unchanged.
        /// </remarks>
        /// <param name="contentPath">The virtual path of the content.</param>
        /// <returns>The application absolute path.</returns>
        string Content(string contentPath);

        /// <summary>
        /// Returns a value that indicates whether the URL is local. An URL with an absolute path is considered local
        /// if it does not have a host/authority part. URLs using the virtual paths ('~/') are also local.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if the URL is local; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <para>
        /// For example, the following URLs are considered local:
        /// /Views/Default/Index.html
        /// ~/Index.html
        /// </para>
        /// <para>
        /// The following URLs are non-local:
        /// ../Index.html
        /// http://www.contoso.com/
        /// http://localhost/Index.html
        /// </para>
        /// </example>
        bool IsLocalUrl(string url);

        /// <summary>
        /// Generates a fully qualified or absolute URL specified by <see cref="Mvc.UrlRouteContext"/>, which
        /// contains the route name, the route values, protocol to use, host name and fragment.
        /// </summary>
        /// <param name="routeContext">The context object for the generated URLs for a route.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        string RouteUrl([NotNull] UrlRouteContext routeContext);

        /// <summary>
        /// Generates an absolute URL using the specified route name and values.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate the URL.</param>
        /// <param name="values">An object that contains the route values.</param>
        /// <returns>The generated absolute URL.</returns>
        /// <remarks>
        /// The protocol and host is obtained from the current request.
        /// </remarks>
        string Link(string routeName, object values);
    }
}
