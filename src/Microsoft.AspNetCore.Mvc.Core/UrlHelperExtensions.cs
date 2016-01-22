// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method.
        /// </summary>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(this IUrlHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(
                action: null,
                controller: null,
                values: null,
                protocol: null,
                host: null,
                fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(this IUrlHelper helper, string action)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller: null, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// and route values.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(this IUrlHelper helper, string action, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller: null, values: values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// and controller name.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(this IUrlHelper helper, string action, string controller)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// controller name, and route values.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(this IUrlHelper helper, string action, string controller, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// controller name, route values, and protocol to use.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// controller name, route values, protocol to use, and host name.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol,
            string host)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol, host, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for an action method by using the specified action name,
        /// controller name, route values, protocol to use, host name and fragment.
        /// </summary>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The fully qualified or absolute URL to an action method.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol,
            string host,
            string fragment)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(new UrlActionContext()
            {
                Action = action,
                Controller = controller,
                Host = host,
                Values = values,
                Protocol = protocol,
                Fragment = fragment
            });
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName: null, values: values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route name.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, string routeName)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values by
        /// using the specified route name.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, string routeName, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values by
        /// using the specified route name, and protocol to use.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values by
        /// using the specified route name, protocol to use, and host name.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol,
            string host)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol, host, fragment: null);
        }

        /// <summary>
        /// Generates a fully qualified or absolute URL for the specified route values by
        /// using the specified route name, protocol to use, host name and fragment.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The fully qualified or absolute URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol,
            string host,
            string fragment)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(new UrlRouteContext()
            {
                RouteName = routeName,
                Values = values,
                Protocol = protocol,
                Host = host,
                Fragment = fragment
            });
        }
    }
}
