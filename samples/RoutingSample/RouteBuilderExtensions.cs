// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace RoutingSample
{
    public static class RouteBuilderExtensions
    {
        public static void AddPrefixRoute(this IRouteBuilder builder, string prefix)
        {
            builder.Routes.Add(new PrefixRoute(builder.Endpoint, prefix));
        }
    }
}
