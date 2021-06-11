// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class EndpointMethodInfoApiDescriptionProviderTest
    {
        [Fact]
        public void ApiDescription_MultipleCreatedForMultipleHttpMethods()
        {
            Action action = () => { };
            var httpMethods = new string[] { "FOO", "BAR" };

            var apiDescriptions = GetApiDescriptions(action.Method, httpMethods);

            Assert.Equal(httpMethods.Length, apiDescriptions.Count);
        }

        [Fact]
        public void ApiDescription_NotCreatedIfNoHttpMethods()
        {
            Action action = () => { };
            var emptyHttpMethods = new string[0];

            var apiDescriptions = GetApiDescriptions(action.Method, emptyHttpMethods);

            Assert.Empty(apiDescriptions);
        }

        private IList<ApiDescription> GetApiDescriptions(
            MethodInfo methodInfo,
            IEnumerable<string> httpMethods = null,
            string pattern = null)
        {
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? new[] { "GET" });
            var endpointMetadata = new EndpointMetadataCollection(methodInfo, httpMethodMetadata);
            var routePattern = RoutePatternFactory.Pattern(pattern ?? "/");

            var endpoint = new RouteEndpoint(httpContext => Task.CompletedTask, routePattern, 0, endpointMetadata, null);
            var endpointDataSource = new DefaultEndpointDataSource(endpoint);

            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource);

            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            return context.Results;
        }
    }
}
