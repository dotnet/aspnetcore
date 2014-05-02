// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
