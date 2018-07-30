// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGenerator
    {
        public string GetLink(object values)
        {
            return GetLink(httpContext: null, routeName: null, values, options: null);
        }

        public string GetLink(object values, LinkOptions options)
        {
            return GetLink(httpContext: null, routeName: null, values, options);
        }

        public bool TryGetLink(object values, out string link)
        {
            return TryGetLink(httpContext: null, routeName: null, values, options: null, out link);
        }

        public bool TryGetLink(object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext: null, routeName: null, values, options, out link);
        }

        public string GetLink(HttpContext httpContext, object values)
        {
            return GetLink(httpContext, routeName: null, values, options: null);
        }

        public bool TryGetLink(HttpContext httpContext, object values, out string link)
        {
            return TryGetLink(httpContext, routeName: null, values, options: null, out link);
        }

        public string GetLink(HttpContext httpContext, object values, LinkOptions options)
        {
            return GetLink(httpContext, routeName: null, values, options);
        }

        public bool TryGetLink(HttpContext httpContext, object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext, routeName: null, values, options, out link);
        }

        public string GetLink(string routeName, object values)
        {
            return GetLink(httpContext: null, routeName, values, options: null);
        }

        public bool TryGetLink(string routeName, object values, out string link)
        {
            return TryGetLink(httpContext: null, routeName, values, options: null, out link);
        }

        public string GetLink(string routeName, object values, LinkOptions options)
        {
            return GetLink(httpContext: null, routeName, values, options);
        }

        public bool TryGetLink(string routeName, object values, LinkOptions options, out string link)
        {
            return TryGetLink(httpContext: null, routeName, values, options, out link);
        }

        public string GetLink(HttpContext httpContext, string routeName, object values)
        {
            return GetLink(httpContext, routeName, values, options: null);
        }

        public bool TryGetLink(HttpContext httpContext, string routeName, object values, out string link)
        {
            return TryGetLink(httpContext, routeName, values, options: null, out link);
        }

        public string GetLink(HttpContext httpContext, string routeName, object values, LinkOptions options)
        {
            if (TryGetLink(httpContext, routeName, values, options, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        public abstract bool TryGetLink(
            HttpContext httpContext,
            string routeName,
            object values,
            LinkOptions options,
            out string link);

        public string GetLinkByAddress<TAddress>(TAddress address, object values)
        {
            return GetLinkByAddress(address, httpContext: null, values, options: null);
        }

        public bool TryGetLinkByAddress<TAddress>(TAddress address, object values, out string link)
        {
            return TryGetLinkByAddress(address, values, options: null, out link);
        }

        public string GetLinkByAddress<TAddress>(TAddress address, object values, LinkOptions options)
        {
            return GetLinkByAddress(address, httpContext: null, values, options);
        }

        public bool TryGetLinkByAddress<TAddress>(
            TAddress address,
            object values,
            LinkOptions options,
            out string link)
        {
            return TryGetLinkByAddress(address, httpContext: null, values, options, out link);
        }

        public string GetLinkByAddress<TAddress>(TAddress address, HttpContext httpContext, object values)
        {
            return GetLinkByAddress(address, httpContext, values, options: null);
        }

        public bool TryGetLinkByAddress<TAddress>(
            TAddress address,
            HttpContext httpContext,
            object values,
            out string link)
        {
            return TryGetLinkByAddress(address, httpContext, values, options: null, out link);
        }

        public string GetLinkByAddress<TAddress>(
            TAddress address,
            HttpContext httpContext,
            object values,
            LinkOptions options)
        {
            if (TryGetLinkByAddress(address, httpContext, values, options, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        public abstract bool TryGetLinkByAddress<TAddress>(
            TAddress address,
            HttpContext httpContext,
            object values,
            LinkOptions options,
            out string link);
    }
}
