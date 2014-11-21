// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    internal class PrefixRoute : IRouter
    {
        private readonly IRouter _target;
        private readonly string _prefix;

        public PrefixRoute(IRouter target, string prefix)
        {
            _target = target;

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

        public async Task RouteAsync(RouteContext context)
        {
            var requestPath = context.HttpContext.Request.Path.Value ?? string.Empty;
            if (requestPath.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (requestPath.Length > _prefix.Length)
                {
                    var lastCharacter = requestPath[_prefix.Length];
                    if (lastCharacter != '/' && lastCharacter != '#' && lastCharacter != '?')
                    {
                        return;
                    }
                }

                await _target.RouteAsync(context);
            }
        }

        public string GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }
    }
}
