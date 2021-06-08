// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal class EndpointMethodInfoApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly EndpointDataSource _endpointDataSource;

        // Executes before MVC's DefaultApiDescriptionProvider and GrpcHttpApiDescriptionProvider
        public int Order => -1100;

        public EndpointMethodInfoApiDescriptionProvider(EndpointDataSource endpointDataSource)
        {
            _endpointDataSource = endpointDataSource;
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var endpoint in _endpointDataSource.Endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint
                    && routeEndpoint.Metadata.GetMetadata<MethodInfo>() is { } methodInfo
                    && routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is { } httpMethodMetadata)
                {
                    foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                    {
                        context.Results.Add(CreateApiDescription(routeEndpoint.RoutePattern, httpMethod, methodInfo));
                    }
                }
            }
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        private static ApiDescription CreateApiDescription(RoutePattern pattern, string httpMethod, MethodInfo actionMethodInfo)
        {
            var apiDescription = new ApiDescription
            {
                HttpMethod = httpMethod,
                RelativePath = pattern.ToString(),
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string?>
                    {
                        // Swagger uses this to group endpoints together.
                        // For now, put all endpoints configured with Map(Delegate) together.
                        // TODO: Use some other metadata for this.
                        ["controller"] = "Map"
                    },
                },
            };

            //var returnType = actionMethodInfo.ReturnType.
            //if (actionMethodInfo.)

            return apiDescription;
        }
    }
}
