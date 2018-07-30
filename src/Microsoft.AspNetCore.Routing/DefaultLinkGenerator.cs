// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ILogger<DefaultLinkGenerator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RouteOptions _options;

        public DefaultLinkGenerator(
            MatchProcessorFactory matchProcessorFactory,
            ObjectPool<UriBuildingContext> uriBuildingContextPool,
            IOptions<RouteOptions> routeOptions,
            ILogger<DefaultLinkGenerator> logger,
            IServiceProvider serviceProvider)
        {
            _matchProcessorFactory = matchProcessorFactory;
            _uriBuildingContextPool = uriBuildingContextPool;
            _options = routeOptions.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override bool TryGetLink(
            HttpContext httpContext,
            string routeName,
            object values,
            LinkOptions options,
            out string link)
        {
            return TryGetLinkByRouteValues(
               httpContext,
               routeName,
               values,
               options,
               out link);
        }

        public override bool TryGetLinkByAddress<TAddress>(
            TAddress address,
            HttpContext httpContext,
            object values,
            LinkOptions options,
            out string link)
        {
            return TryGetLinkByAddressInternal(
                address,
                httpContext,
                explicitValues: values,
                ambientValues: GetAmbientValues(httpContext),
                options,
                out link);
        }

        private bool TryGetLinkByRouteValues(
            HttpContext httpContext,
            string routeName,
            object values,
            LinkOptions options,
            out string link)
        {
            var ambientValues = GetAmbientValues(httpContext);

            var address = new RouteValuesAddress
            {
                RouteName = routeName,
                ExplicitValues = new RouteValueDictionary(values),
                AmbientValues = ambientValues
            };

            return TryGetLinkByAddressInternal(
                address,
                httpContext,
                explicitValues: values,
                ambientValues: ambientValues,
                options,
                out link);
        }

        private bool TryGetLinkByAddressInternal<TAddress>(
            TAddress address,
            HttpContext httpContext,
            object explicitValues,
            RouteValueDictionary ambientValues,
            LinkOptions options,
            out string link)
        {
            link = null;

            var endpointFinder = _serviceProvider.GetRequiredService<IEndpointFinder<TAddress>>();
            var endpoints = endpointFinder.FindEndpoints(address);
            if (endpoints == null)
            {
                return false;
            }

            var matcherEndpoints = endpoints.OfType<MatcherEndpoint>();
            if (!matcherEndpoints.Any())
            {
                return false;
            }

            foreach (var endpoint in matcherEndpoints)
            {
                link = GetLink(endpoint);
                if (link != null)
                {
                    return true;
                }
            }

            return false;

            string GetLink(MatcherEndpoint endpoint)
            {
                var templateBinder = new TemplateBinder(
                    UrlEncoder.Default,
                    _uriBuildingContextPool,
                    new RouteTemplate(endpoint.RoutePattern),
                    new RouteValueDictionary(endpoint.RoutePattern.Defaults));

                var templateValuesResult = templateBinder.GetValues(
                    ambientValues: ambientValues,
                    explicitValues: new RouteValueDictionary(explicitValues),
                    requiredKeys: endpoint.RequiredValues.Keys);
                if (templateValuesResult == null)
                {
                    // We're missing one of the required values for this route.
                    return null;
                }

                if (!MatchesConstraints(httpContext, endpoint, templateValuesResult.CombinedValues))
                {
                    return null;
                }

                var url = templateBinder.BindValues(templateValuesResult.AcceptedValues);
                return Normalize(url, options);
            }
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

        private string Normalize(string url, LinkOptions options)
        {
            var lowercaseUrls = options?.LowercaseUrls ?? _options.LowercaseUrls;
            var lowercaseQueryStrings = options?.LowercaseQueryStrings ?? _options.LowercaseQueryStrings;
            var appendTrailingSlash = options?.AppendTrailingSlash ?? _options.AppendTrailingSlash;

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

        private RouteValueDictionary GetAmbientValues(HttpContext httpContext)
        {
            if (httpContext != null)
            {
                var feature = httpContext.Features.Get<IEndpointFeature>();
                if (feature != null)
                {
                    return feature.Values;
                }
            }
            return null;
        }
    }
}
