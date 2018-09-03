// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultLinkGenerationTemplate : LinkGenerationTemplate
    {
        public DefaultLinkGenerationTemplate(
            DefaultLinkGenerator linkGenerator,
            IEnumerable<RouteEndpoint> endpoints,
            HttpContext httpContext,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues)
        {
            LinkGenerator = linkGenerator;
            Endpoints = endpoints;
            HttpContext = httpContext;
            EarlierExplicitValues = explicitValues;
            AmbientValues = ambientValues;
        }

        internal DefaultLinkGenerator LinkGenerator { get; }

        internal IEnumerable<RouteEndpoint> Endpoints { get; }

        internal HttpContext HttpContext { get; }

        internal RouteValueDictionary EarlierExplicitValues { get; }

        internal RouteValueDictionary AmbientValues { get; }

        public override string MakeUrl(object values, LinkOptions options)
        {
            var currentValues = new RouteValueDictionary(values);
            var mergedValuesDictionary = new RouteValueDictionary(EarlierExplicitValues);

            foreach (var kvp in currentValues)
            {
                mergedValuesDictionary[kvp.Key] = kvp.Value;
            }

            foreach (var endpoint in Endpoints)
            {
                var link = LinkGenerator.MakeLink(
                    HttpContext,
                    endpoint,
                    AmbientValues,
                    mergedValuesDictionary,
                    options);
                if (link != null)
                {
                    return link;
                }
            }
            return null;
        }
    }
}
