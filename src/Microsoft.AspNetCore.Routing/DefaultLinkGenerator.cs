// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class DefaultLinkGenerator : LinkGenerator
    {
        private static readonly char[] UrlQueryDelimiters = new char[] { '?', '#' };
        private readonly ParameterPolicyFactory _parameterPolicyFactory;
        private readonly ObjectPool<UriBuildingContext> _uriBuildingContextPool;
        private readonly ILogger<DefaultLinkGenerator> _logger;
        private readonly IServiceProvider _serviceProvider;

        // A LinkOptions object initialized with the values from RouteOptions
        // Used when the user didn't specify something more global.
        private readonly LinkOptions _globalLinkOptions;

        public DefaultLinkGenerator(
            ParameterPolicyFactory parameterPolicyFactory,
            ObjectPool<UriBuildingContext> uriBuildingContextPool,
            IOptions<RouteOptions> routeOptions,
            ILogger<DefaultLinkGenerator> logger,
            IServiceProvider serviceProvider)
        {
            _parameterPolicyFactory = parameterPolicyFactory;
            _uriBuildingContextPool = uriBuildingContextPool;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _globalLinkOptions = new LinkOptions()
            {
                AppendTrailingSlash = routeOptions.Value.AppendTrailingSlash,
                LowercaseQueryStrings = routeOptions.Value.LowercaseQueryStrings,
                LowercaseUrls = routeOptions.Value.LowercaseUrls,
            };
        }

        public override string GetPathByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            RouteValueDictionary values,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var endpoints = GetEndpoints(address);
            if (endpoints.Count == 0)
            {
                return null;
            }

            return GetPathByEndpoints(
                endpoints,
                GetAmbientValues(httpContext),
                values,
                httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetPathByAddress<TAddress>(
            TAddress address,
            RouteValueDictionary values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            var endpoints = GetEndpoints(address);
            if (endpoints.Count == 0)
            {
                return null;
            }

            return GetPathByEndpoints(
                endpoints,
                ambientValues: null,
                values,
                pathBase,
                fragment,
                options);
        }

        public override string GetUriByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            RouteValueDictionary values,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var endpoints = GetEndpoints(address);
            if (endpoints.Count == 0)
            {
                return null;
            }

            return GetUriByEndpoints(
                endpoints,
                GetAmbientValues(httpContext),
                values,
                httpContext.Request.Scheme,
                httpContext.Request.Host,
                httpContext.Request.PathBase,
                fragment,
                options);
        }

        public override string GetUriByAddress<TAddress>(
            TAddress address,
            RouteValueDictionary values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (!host.HasValue)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var endpoints = GetEndpoints(address);
            if (endpoints.Count == 0)
            {
                return null;
            }

            return GetUriByEndpoints(
                endpoints,
                ambientValues: null,
                values,
                scheme,
                host,
                pathBase,
                fragment,
                options);
        }

        public override LinkGenerationTemplate GetTemplateByAddress<TAddress>(TAddress address)
        {
            var endpoints = GetEndpoints(address);
            if (endpoints.Count == 0)
            {
                return null;
            }

            return new DefaultLinkGenerationTemplate(this, endpoints);
        }

        private List<RouteEndpoint> GetEndpoints<TAddress>(TAddress address)
        {
            var addressingScheme = _serviceProvider.GetRequiredService<IEndpointFinder<TAddress>>();
            return addressingScheme.FindEndpoints(address).OfType<RouteEndpoint>().ToList();
        }

        // Also called from DefaultLinkGenerationTemplate
        public string GetPathByEndpoints(
            List<RouteEndpoint> endpoints,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values,
            PathString pathBase,
            FragmentString fragment,
            LinkOptions options)
        {
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                if (TryProcessTemplate(
                    httpContext: null,
                    endpoint,
                    ambientValues: ambientValues,
                    values,
                    options,
                    out var result))
                {

                    return UriHelper.BuildRelative(
                        pathBase,
                        result.path,
                        result.query,
                        fragment);
                }
            }

            return null;
        }

        // Also called from DefaultLinkGenerationTemplate
        public string GetUriByEndpoints(
            List<RouteEndpoint> endpoints,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values,
            string scheme,
            HostString host,
            PathString pathBase,
            FragmentString fragment,
            LinkOptions options)
        {
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                if (TryProcessTemplate(
                    httpContext: null,
                    endpoint,
                    ambientValues: ambientValues,
                    values,
                    options,
                    out var result))
                {
                    return UriHelper.BuildAbsolute(
                        scheme,
                        host,
                        pathBase,
                        result.path,
                        result.query,
                        fragment);
                }
            }

            return null;
        }

        // Internal for testing
        internal bool TryProcessTemplate(
            HttpContext httpContext,
            RouteEndpoint endpoint,
            RouteValueDictionary ambientValues,
            RouteValueDictionary explicitValues,
            LinkOptions options,
            out (PathString path, QueryString query) result)
        {
            var templateBinder = new TemplateBinder(
                UrlEncoder.Default,
                _uriBuildingContextPool,
                endpoint.RoutePattern,
                new RouteValueDictionary(endpoint.RoutePattern.Defaults));

            var routeValuesAddressMetadata = endpoint.Metadata.GetMetadata<IRouteValuesAddressMetadata>();
            var templateValuesResult = templateBinder.GetValues(
                ambientValues: ambientValues,
                explicitValues: explicitValues,
                requiredKeys: routeValuesAddressMetadata?.RequiredValues.Keys);
            if (templateValuesResult == null)
            {
                // We're missing one of the required values for this route.
                result = default;
                return false;
            }

            if (!MatchesConstraints(httpContext, endpoint, templateValuesResult.CombinedValues))
            {
                result = default;
                return false;
            }

            if (!templateBinder.TryBindValues(templateValuesResult.AcceptedValues, options, _globalLinkOptions, out result))
            {
                return false;
            }

            return true;
        }

        private bool MatchesConstraints(
            HttpContext httpContext,
            RouteEndpoint endpoint,
            RouteValueDictionary routeValues)
        {
            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            foreach (var kvp in endpoint.RoutePattern.ParameterPolicies)
            {
                var parameter = endpoint.RoutePattern.GetParameter(kvp.Key); // may be null, that's ok
                var constraintReferences = kvp.Value;
                for (var i = 0; i < constraintReferences.Count; i++)
                {
                    var constraintReference = constraintReferences[i];
                    var parameterPolicy = _parameterPolicyFactory.Create(parameter, constraintReference);
                    if (parameterPolicy is IRouteConstraint routeConstraint
                        && !routeConstraint.Match(httpContext, NullRouter.Instance, kvp.Key, routeValues, RouteDirection.UrlGeneration))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Also called from DefaultLinkGenerationTemplate
        public static RouteValueDictionary GetAmbientValues(HttpContext httpContext)
        {
            return httpContext?.Features.Get<IRouteValuesFeature>()?.RouteValues;
        }
    }
}
