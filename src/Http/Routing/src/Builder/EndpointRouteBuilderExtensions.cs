// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        // Avoid creating a new array every call
        private static readonly string[] GetVerb = new[] { "GET" };
        private static readonly string[] PostVerb = new[] { "POST" };
        private static readonly string[] PutVerb = new[] { "PUT" };
        private static readonly string[] DeleteVerb = new[] { "DELETE" };

        #region MapVerbs
        public static IEndpointConventionBuilder MapGet(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, GetVerb, metadata);
        }

        public static IEndpointConventionBuilder MapGet(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, GetVerb, metadata);
        }

        public static IEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, PostVerb, metadata);
        }

        public static IEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, PostVerb, metadata);
        }

        public static IEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, PutVerb, metadata);
        }

        public static IEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, PutVerb, metadata);
        }

        public static IEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, DeleteVerb, metadata);
        }

        public static IEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName, requestDelegate, DeleteVerb, metadata);
        }

        public static IEndpointConventionBuilder MapVerbs(
           this IEndpointRouteBuilder builder,
           string pattern,
           RequestDelegate requestDelegate,
           IList<string> httpMethods,
           params object[] metadata)
        {
            return MapVerbs(builder, pattern, displayName: null, requestDelegate, httpMethods, metadata);
        }

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
        #endregion

        #region Map
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            string pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, RoutePatternFactory.Parse(pattern), pattern, requestDelegate, metadata);
        }

        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            string pattern,
            string displayName,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, RoutePatternFactory.Parse(pattern), displayName, requestDelegate, metadata);
        }

        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder builder,
            RoutePattern pattern,
            RequestDelegate requestDelegate,
            params object[] metadata)
        {
            return Map(builder, pattern, pattern.RawText ?? pattern.DebuggerToString(), requestDelegate, metadata);
        }

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
                defaultOrder);
            routeEndpointBuilder.DisplayName = displayName;
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
        #endregion
    }
}
