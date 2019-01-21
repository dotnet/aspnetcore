// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add endpoints.
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        // Avoid creating a new array every call
        private static readonly string[] GetVerb = new[] { "GET" };
        private static readonly string[] PostVerb = new[] { "POST" };
        private static readonly string[] PutVerb = new[] { "PUT" };
        private static readonly string[] DeleteVerb = new[] { "DELETE" };

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapGet(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, GetVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapGet(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, GetVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, PostVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, PostVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, PutVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, PutVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, DeleteVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, DeleteVerb, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified HTTP methods and pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapVerbs(
           this IEndpointRouteBuilder builder,
           string pattern,
           RequestDelegate requestDelegate,
           IList<string> httpMethods,
           params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, httpMethods, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified HTTP methods and pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapVerbs(
           this IEndpointRouteBuilder builder,
           string pattern,
           string displayName,
           RequestDelegate requestDelegate,
           IList<string> httpMethods,
           params object[] metadata)
        {
            if (httpMethods == null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            var resolvedMetadata = new List<object>();
            resolvedMetadata.Add(new HttpMethodMetadata(httpMethods));
            if (metadata != null)
            {
                resolvedMetadata.AddRange(metadata);
            }

            return Map(builder, pattern, displayName ?? $"{pattern} HTTP: {string.Join(", ", httpMethods)}", requestDelegate, metadata: resolvedMetadata.ToArray());
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, RoutePatternFactory.Parse(pattern), pattern, requestDelegate, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, RoutePatternFactory.Parse(pattern), displayName, requestDelegate, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            RoutePattern pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, pattern, pattern.RawText ?? pattern.DebuggerToString(), requestDelegate, metadata);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="displayName">The display name for the endpoint.</param>
        /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
        /// <param name="metadata">Metadata that is added to the endpoint.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            RoutePattern pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            const int defaultOrder = 0;

            var routeEndpointBuilder = new RouteEndpointBuilder(
                requestDelegate,
                pattern,
                defaultOrder)
            {
                DisplayName = displayName
            };

            // Add delegate attributes as metadata
            foreach (var attribute in requestDelegate.Method.GetCustomAttributes())
            {
                routeEndpointBuilder.Metadata.Add(attribute);
            }

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    routeEndpointBuilder.Metadata.Add(item);
                }
            }

            var modelEndpointDataSource = builder.DataSources.OfType<ModelEndpointDataSource>().FirstOrDefault();

            if (modelEndpointDataSource == null)
            {
                modelEndpointDataSource = new ModelEndpointDataSource();
                builder.DataSources.Add(modelEndpointDataSource);
            }

            return modelEndpointDataSource.AddEndpointBuilder(routeEndpointBuilder);
        }
    }
}
