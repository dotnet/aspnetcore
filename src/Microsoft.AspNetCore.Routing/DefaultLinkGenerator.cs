// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing
{
    public class DefaultLinkGenerator : ILinkGenerator
    {
        private readonly IEndpointFinder _endpointFinder;
        private readonly ObjectPool<UriBuildingContext> _uriBuildingContextPool;
        private readonly ILogger<DefaultLinkGenerator> _logger;

        public DefaultLinkGenerator(
            IEndpointFinder endpointFinder,
            ObjectPool<UriBuildingContext> uriBuildingContextPool,
            ILogger<DefaultLinkGenerator> logger)
        {
            _endpointFinder = endpointFinder;
            _uriBuildingContextPool = uriBuildingContextPool;
            _logger = logger;
        }

        public string GetLink(LinkGeneratorContext context)
        {
            if (TryGetLink(context, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        public bool TryGetLink(LinkGeneratorContext context, out string link)
        {
            var address = context.Address;
            var endpoints = _endpointFinder.FindEndpoints(address);
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
                link = GetLink(endpoint.ParsedTemlate, endpoint.Values, context);
                if (link != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetLink(
            RouteTemplate template,
            IReadOnlyDictionary<string, object> defaultValues,
            LinkGeneratorContext context)
        {
            var defaults = new RouteValueDictionary(defaultValues);
            var templateBinder = new TemplateBinder(
                UrlEncoder.Default,
                _uriBuildingContextPool,
                template,
                defaults);

            var values = templateBinder.GetValues(
                new RouteValueDictionary(context.AmbientValues),
                new RouteValueDictionary(context.SuppliedValues));
            if (values == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            //TODO: route constraint matching here

            return templateBinder.BindValues(values.AcceptedValues);
        }
    }
}
