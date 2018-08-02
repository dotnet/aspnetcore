// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultLinkGenerator : LinkGenerator
    {
        private readonly static char[] UrlQueryDelimiters = new char[] { '?', '#' };
        private readonly MatchProcessorFactory _matchProcessorFactory;
        private readonly ObjectPool<UriBuildingContext> _uriBuildingContextPool;
        private readonly RouteOptions _options;
        private readonly ILogger<DefaultLinkGenerator> _logger;

        public DefaultLinkGenerator(
            MatchProcessorFactory matchProcessorFactory,
            ObjectPool<UriBuildingContext> uriBuildingContextPool,
            IOptions<RouteOptions> routeOptions,
            ILogger<DefaultLinkGenerator> logger)
        {
            _matchProcessorFactory = matchProcessorFactory;
            _uriBuildingContextPool = uriBuildingContextPool;
            _options = routeOptions.Value;
            _logger = logger;
        }

        public override string GetLink(LinkGeneratorContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (TryGetLink(context, out var link))
            {
                return link;
            }

            throw new InvalidOperationException("Could not find a matching endpoint to generate a link.");
        }

        public override bool TryGetLink(LinkGeneratorContext context, out string link)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            link = null;

            if (context.Endpoints == null)
            {
                return false;
            }

            var matcherEndpoints = context.Endpoints.OfType<MatcherEndpoint>();
            if (!matcherEndpoints.Any())
            {
                //todo:log here
                return false;
            }

            foreach (var endpoint in matcherEndpoints)
            {
                link = GetLink(endpoint, context);
                if (link != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetLink(MatcherEndpoint endpoint, LinkGeneratorContext context)
        {
            var templateBinder = new TemplateBinder(
                UrlEncoder.Default,
                _uriBuildingContextPool,
                new RouteTemplate(endpoint.RoutePattern),
                new RouteValueDictionary(endpoint.RoutePattern.Defaults));

            var templateValuesResult = templateBinder.GetValues(
                ambientValues: context.AmbientValues,
                explicitValues: context.ExplicitValues,
                endpoint.RequiredValues.Keys);

            if (templateValuesResult == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            if (!MatchesConstraints(context.HttpContext, endpoint, templateValuesResult.CombinedValues))
            {
                return null;
            }

            var url = templateBinder.BindValues(templateValuesResult.AcceptedValues);
            return Normalize(context, url);
        }

        private bool MatchesConstraints(
            HttpContext httpContext,
            MatcherEndpoint endpoint,
            RouteValueDictionary routeValues)
        {
            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            foreach (var kvp in endpoint.RoutePattern.Constraints)
            {
                var parameter = endpoint.RoutePattern.GetParameter(kvp.Key); // may be null, that's ok
                var constraintReferences = kvp.Value;
                for (var i = 0; i < constraintReferences.Count; i++)
                {
                    var constraintReference = constraintReferences[i];
                    var matchProcessor = _matchProcessorFactory.Create(parameter, constraintReference);
                    if (!matchProcessor.ProcessOutbound(httpContext, routeValues))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private string Normalize(LinkGeneratorContext context, string url)
        {
            var lowercaseUrls = context.LowercaseUrls.HasValue ? context.LowercaseUrls.Value : _options.LowercaseUrls;
            var lowercaseQueryStrings = context.LowercaseQueryStrings.HasValue ?
                context.LowercaseQueryStrings.Value : _options.LowercaseQueryStrings;
            var appendTrailingSlash = context.AppendTrailingSlash.HasValue ?
                context.AppendTrailingSlash.Value : _options.AppendTrailingSlash;

            if (!string.IsNullOrEmpty(url) && (lowercaseUrls || appendTrailingSlash))
            {
                var indexOfSeparator = url.IndexOfAny(UrlQueryDelimiters);
                var urlWithoutQueryString = url;
                var queryString = string.Empty;

                if (indexOfSeparator != -1)
                {
                    urlWithoutQueryString = url.Substring(0, indexOfSeparator);
                    queryString = url.Substring(indexOfSeparator);
                }

                if (lowercaseUrls)
                {
                    urlWithoutQueryString = urlWithoutQueryString.ToLowerInvariant();
                }

                if (lowercaseUrls && lowercaseQueryStrings)
                {
                    queryString = queryString.ToLowerInvariant();
                }

                if (appendTrailingSlash && !urlWithoutQueryString.EndsWith("/", StringComparison.Ordinal))
                {
                    urlWithoutQueryString += "/";
                }

                // queryString will contain the delimiter ? or # as the first character, so it's safe to append.
                url = urlWithoutQueryString + queryString;
            }

            return url;
        }
    }
}
