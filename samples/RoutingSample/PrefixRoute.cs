﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Routing;

namespace RoutingSample
{
    internal class PrefixRoute : IRoute
    {
        private readonly IRouteEndpoint _endpoint;
        private readonly string _prefix;

        public PrefixRoute(IRouteEndpoint endpoint, string prefix)
        {
            _endpoint = endpoint;

            if (prefix == null)
            {
                prefix = "/";
            }
            else if (prefix.Length > 0 && prefix[0] != '/')
            {
                // owin.RequestPath starts with a /
                prefix = "/" + prefix;
            }

            if (prefix.Length > 1 && prefix[prefix.Length - 1] == '/')
            {
                prefix = prefix.Substring(0, prefix.Length - 1);
            }

            _prefix = prefix;
        }

        public RouteMatch Match(RouteContext context)
        {
            if (context.RequestPath.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (context.RequestPath.Length > _prefix.Length)
                {
                    char next = context.RequestPath[_prefix.Length];
                    if (next != '/' && next != '#' && next != '?')
                    {
                        return null;
                    }
                }

                return new RouteMatch(_endpoint);
            }

            return null;
        }
    }
}
