// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class DefaultLinkGenerationTemplate : LinkGenerationTemplate
    {
        public DefaultLinkGenerationTemplate(DefaultLinkGenerator linkGenerator, List<RouteEndpoint> endpoints)
        {
            LinkGenerator = linkGenerator;
            Endpoints = endpoints;
        }

        public DefaultLinkGenerator LinkGenerator { get; }

        public List<RouteEndpoint> Endpoints { get; }

        public override string GetPath(
            HttpContext httpContext,
            object values,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return LinkGenerator.GetPathByEndpoints(
                Endpoints,
                DefaultLinkGenerator.GetAmbientValues(httpContext),
                new RouteValueDictionary(values),
                httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetPath(
            object values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            return LinkGenerator.GetPathByEndpoints(
                Endpoints,
                ambientValues: null,
                new RouteValueDictionary(values),
                pathBase,
                fragment,
                options);
        }

        public override string GetUri(
            HttpContext httpContext,
            object values,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return LinkGenerator.GetUriByEndpoints(
                Endpoints,
                DefaultLinkGenerator.GetAmbientValues(httpContext),
                new RouteValueDictionary(values),
                httpContext.Request.Scheme,
                httpContext.Request.Host,
                httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetUri(
            object values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            return LinkGenerator.GetUriByEndpoints(
                Endpoints,
                ambientValues: null,
                new RouteValueDictionary(values),
                scheme,
                host,
                pathBase,
                fragment,
                options);
        }
    }
}
