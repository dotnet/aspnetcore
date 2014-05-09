// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string Action([NotNull] this IUrlHelper helper)
        {
            return helper.Action(
                action: null, 
                controller: null, 
                values: null, 
                protocol: null, 
                host: null, 
                fragment: null);
        }

        public static string Action([NotNull] this IUrlHelper helper, string action)
        {
            return helper.Action(action, controller: null, values: null, protocol: null, host: null, fragment: null);
        }

        public static string Action([NotNull] this IUrlHelper helper, string action, object values)
        {
            return helper.Action(action, controller: null, values: values, protocol: null, host: null, fragment: null);
        }

        public static string Action([NotNull] this IUrlHelper helper, string action, string controller)
        {
            return helper.Action(action, controller, values: null, protocol: null, host: null, fragment: null);
        }

        public static string Action([NotNull] this IUrlHelper helper, string action, string controller, object values)
        {
            return helper.Action(action, controller, values, protocol: null, host: null, fragment: null);
        }

        public static string Action(
            [NotNull] this IUrlHelper helper, 
            string action, 
            string controller, 
            object values, 
            string protocol)
        {
            return helper.Action(action, controller, values, protocol, host: null, fragment: null);
        }

        public static string Action(
            [NotNull] this IUrlHelper helper, 
            string action, 
            string controller, 
            object values, 
            string protocol, 
            string host)
        {
            return helper.Action(action, controller, values, protocol, host, fragment: null);
        }

        public static string RouteUrl([NotNull] this IUrlHelper helper, object values)
        {
            return helper.RouteUrl(routeName: null, values: values, protocol: null, host: null, fragment: null);
        }

        public static string RouteUrl([NotNull] this IUrlHelper helper, string routeName)
        {
            return helper.RouteUrl(routeName, values: null, protocol: null, host: null, fragment: null);
        }

        public static string RouteUrl([NotNull] this IUrlHelper helper, string routeName, object values)
        {
            return helper.RouteUrl(routeName, values, protocol: null, host: null, fragment: null);
        }

        public static string RouteUrl([NotNull] this IUrlHelper helper, string routeName, object values, string protocol)
        {
            return helper.RouteUrl(routeName, values, protocol, host: null, fragment: null);
        }

        public static string RouteUrl([NotNull] this IUrlHelper helper,
                                      string routeName,
                                      object values,
                                      string protocol,
                                      string host)
        {
            return helper.RouteUrl(routeName, values, protocol, host, fragment: null);
        }
    }
}
