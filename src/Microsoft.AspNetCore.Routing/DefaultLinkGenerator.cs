// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultLinkGenerator : LinkGenerator
    {
        private readonly MatchProcessorFactory _matchProcessorFactory;
        private readonly ObjectPool<UriBuildingContext> _uriBuildingContextPool;
        private readonly ILogger<DefaultLinkGenerator> _logger;

        public DefaultLinkGenerator(
            MatchProcessorFactory matchProcessorFactory,
            ObjectPool<UriBuildingContext> uriBuildingContextPool,
            ILogger<DefaultLinkGenerator> logger)
        {
            _matchProcessorFactory = matchProcessorFactory;
            _uriBuildingContextPool = uriBuildingContextPool;
            _logger = logger;
        }

        public override string GetLink(
            HttpContext httpContext,
            IEnumerable<Endpoint> endpoints,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues)
        {
            if (TryGetLink(httpContext, endpoints, explicitValues, ambientValues, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        public override bool TryGetLink(
            HttpContext httpContext,
            IEnumerable<Endpoint> endpoints,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues,
            out string link)
        {
            link = null;

            if (endpoints == null)
            {
                return false;
            }

            var matcherEndpoints = endpoints.OfType<MatcherEndpoint>();
            if (!matcherEndpoints.Any())
            {
                //todo:log here
                return false;
            }

            foreach (var endpoint in matcherEndpoints)
            {
                link = GetLink(httpContext, endpoint, explicitValues, ambientValues);
                if (link != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetLink(
            HttpContext httpContext,
            MatcherEndpoint endpoint,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues)
        {
            var templateBinder = new TemplateBinder(
                UrlEncoder.Default,
                _uriBuildingContextPool,
                endpoint.ParsedTemplate,
                endpoint.Defaults);

            var templateValuesResult = templateBinder.GetValues(ambientValues, explicitValues);
            if (templateValuesResult == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            if (!Match(httpContext, endpoint, templateValuesResult.CombinedValues))
            {
                return null;
            }

            return templateBinder.BindValues(templateValuesResult.AcceptedValues);
        }

        private bool Match(HttpContext httpContext, MatcherEndpoint endpoint, RouteValueDictionary routeValues)
        {
            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            for (var i = 0; i < endpoint.MatchProcessorReferences.Count; i++)
            {
                var matchProcessorReference = endpoint.MatchProcessorReferences[i];
                var parameter = endpoint.ParsedTemplate.GetParameter(matchProcessorReference.ParameterName);
                if (parameter.IsOptional && !routeValues.ContainsKey(parameter.Name))
                {
                    continue;
                }

                var matchProcessor = _matchProcessorFactory.Create(matchProcessorReference);
                if (!matchProcessor.ProcessOutbound(httpContext, routeValues))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
