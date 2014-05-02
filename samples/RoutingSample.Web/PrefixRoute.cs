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
            if (context.RequestPath.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (context.RequestPath.Length > _prefix.Length)
                {
                    var lastCharacter = context.RequestPath[_prefix.Length];
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
