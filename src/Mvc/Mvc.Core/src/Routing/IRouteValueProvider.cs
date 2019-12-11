// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// <para>
    /// A metadata interface which specifies a route value which is required for the action selector to
    /// choose an action. When applied to an action using attribute routing, the route value will be added
    /// to the <see cref="RouteData.Values"/> when the action is selected.
    /// </para>
    /// <para>
    /// When an <see cref="IRouteValueProvider"/> is used to provide a new route value to an action, all
    /// actions in the application must also have a value associated with that key, or have an implicit value
    /// of <c>null</c>. See remarks for more details.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The typical scheme for action selection in an MVC application is that an action will require the
    /// matching values for its <see cref="ControllerActionDescriptor.ControllerName"/> and
    /// <see cref="ControllerActionDescriptor.ActionName"/>
    /// </para>
    /// <example>
    /// For an action like <c>MyApp.Controllers.HomeController.Index()</c>, in order to be selected, the
    /// <see cref="RouteData.Values"/> must contain the values
    /// { 
    ///     "action": "Index",
    ///     "controller": "Home"
    /// }
    /// </example>
    /// <para>
    /// If areas are in use in the application (see <see cref="AreaAttribute"/> which implements
    /// <see cref="IRouteValueProvider"/>) then all actions are consider either in an area by having a
    /// non-<c>null</c> area value (specified by <see cref="AreaAttribute"/> or another 
    /// <see cref="IRouteValueProvider"/>) or are considered 'outside' of areas by having the value <c>null</c>.
    /// </para>
    /// <example>
    /// Consider an application with two controllers, each with an <c>Index</c> action method:
    ///     - <c>MyApp.Controllers.HomeController.Index()</c>
    ///     - <c>MyApp.Areas.Blog.Controllers.HomeController.Index()</c>
    /// where <c>MyApp.Areas.Blog.Controllers.HomeController</c> has an area attribute
    /// <c>[Area("Blog")]</c>.
    /// 
    /// For <see cref="RouteData.Values"/> like:
    /// { 
    ///     "action": "Index",
    ///     "controller": "Home"
    /// }
    /// 
    /// <c>MyApp.Controllers.HomeController.Index()</c> will be selected.
    /// <c>MyApp.Area.Blog.Controllers.HomeController.Index()</c> is not considered eligible because the
    /// <see cref="RouteData.Values"/> does not contain the value 'Blog' for 'area'.
    /// 
    /// For <see cref="RouteData.Values"/> like:
    /// {
    ///     "area": "Blog",
    ///     "action": "Index",
    ///     "controller": "Home"
    /// }
    /// 
    /// <c>MyApp.Area.Blog.Controllers.HomeController.Index()</c> will be selected.
    /// <c>MyApp.Controllers.HomeController.Index()</c> is not considered eligible because the route values
    /// contain a value for 'area'. <c>MyApp.Controllers.HomeController.Index()</c> cannot match any value
    /// for 'area' other than <c>null</c>.
    /// </example>
    /// </remarks>
    public interface IRouteValueProvider
    {
        /// <summary>
        /// The route value key.
        /// </summary>
        string RouteKey { get; }

        /// <summary>
        /// The route value. If <c>null</c> or empty, requires the route value associated with <see cref="RouteKey"/>
        /// to be missing or <c>null</c>.
        /// </summary>
        string RouteValue { get; }
    }
}
