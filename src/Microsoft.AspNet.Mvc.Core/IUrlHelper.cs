// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Defines the contract for the helper to build URLs for ASP.NET MVC within an application.
    /// </summary>
    public interface IUrlHelper
    {
        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name, 
        /// controller name, route values, protocol to use, host name and fragment.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains the parameters for a route.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        string Action(string action, string controller, object values, string protocol, string host, string fragment);

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
        /// Returns a value that indicates whether the URL is local.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>true if the URL is local; otherwise, false.</returns>
        bool IsLocalUrl(string url);

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values by 
        /// using the specified route name, protocol to use, host name and fragment.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains the parameters for a route.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        string RouteUrl(string routeName, object values, string protocol, string host, string fragment);
    }
}
