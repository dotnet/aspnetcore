// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class DefaultLinkGenerationTemplate : LinkGenerationTemplate
    {
        public DefaultLinkGenerationTemplate(DefaultLinkGenerator linkGenerator, List<RouteEndpoint> endpoints, LinkGenerationTemplateOptions options)
        {
            LinkGenerator = linkGenerator;
            Endpoints = endpoints;
            Options = options;
        }

        public DefaultLinkGenerator LinkGenerator { get; }

        public List<RouteEndpoint> Endpoints { get; }

        public LinkGenerationTemplateOptions Options { get; }

        public override string GetPath(
            HttpContext httpContext,
            object values,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return LinkGenerator.GetPathByEndpoints(
                Endpoints,
                new RouteValueDictionary(values),
                GetAmbientValues(httpContext),
                pathBase ?? httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetPath(
            object values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            return LinkGenerator.GetPathByEndpoints(
                Endpoints,
                new RouteValueDictionary(values),
                ambientValues: null,
                pathBase: pathBase,
                fragment: fragment,
                options: options);
        }

        public override string GetUri(
            HttpContext httpContext,
            object values,
            string scheme = default,
            HostString? host = default,
            PathString? pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return LinkGenerator.GetUriByEndpoints(
                Endpoints,
                new RouteValueDictionary(values),
                GetAmbientValues(httpContext),
                scheme ?? httpContext.Request.Scheme,
                host ?? httpContext.Request.Host,
                pathBase ?? httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetUri(
            object values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = default)
        {
            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentException("A scheme must be provided.", nameof(scheme));
            }

            if (!host.HasValue)
            {
                throw new ArgumentException("A host must be provided.", nameof(host));
            }

            return LinkGenerator.GetUriByEndpoints(
                Endpoints,
                new RouteValueDictionary(values),
                ambientValues: null,
                scheme: scheme,
                host: host,
                pathBase: pathBase,
                fragment: fragment,
                options: options);
        }

        private RouteValueDictionary GetAmbientValues(HttpContext httpContext)
        {
            return (Options?.UseAmbientValues ?? false) ? DefaultLinkGenerator.GetAmbientValues(httpContext) : null;
        }
    }
}
