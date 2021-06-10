// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Internal;

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
                    // REVIEW: Should we add an ApiDescription for endpoints without IHttpMethodMetadata? Swagger doesn't handle
                    // a null HttpMethod even though it's nullable on ApiDescription, so we'd need to define "default" HTTP methods.
                    // In practice, the Delegate will be called for any HTTP method if there is no IHttpMethodMetadata.
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
                RelativePath = pattern.RawText?.TrimStart('/'),
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues =
                    {
                        // Swagger uses this to group endpoints together.
                        // For now, put all endpoints configured with Map(Delegate) together.
                        // TODO: Use some other metadata for this.
                        ["controller"] = "Map"
                    },
                },
            };

            foreach (var parameter in actionMethodInfo.GetParameters())
            {
                apiDescription.ParameterDescriptions.Add(CreateApiParameterDescription(parameter));
            }

            var responseType = actionMethodInfo.ReturnType;

            if (AwaitableInfo.IsTypeAwaitable(responseType, out var awaitableInfo))
            {
                responseType = awaitableInfo.ResultType;
            }

            responseType = Nullable.GetUnderlyingType(responseType) ?? responseType;

            if (CreateApiResponseType(responseType) is { } apiResponseType)
            {
                apiDescription.SupportedResponseTypes.Add(apiResponseType);
            }

            return apiDescription;
        }

        private static ApiParameterDescription CreateApiParameterDescription(ParameterInfo parameter)
        {
            var parameterType = parameter.ParameterType;

            var (source, name) = GetBindingSourceAndName(parameter);

            return new ApiParameterDescription
            {
                Name = name,
                ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(parameterType)),
                Source = source,
                DefaultValue = parameter.DefaultValue,
            };
        }

        // TODO: Share more of this logic with RequestDelegateFactory.CreateArgument(...) using RequestDelegateFactoryUtilities
        // which is shared source.
        private static (BindingSource, string) GetBindingSourceAndName(ParameterInfo parameter)
        {
            var attributes = parameter.GetCustomAttributes();

            if (attributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
            {
                return (BindingSource.Path, routeAttribute.Name ?? parameter.Name ?? string.Empty);
            }
            else if (attributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
            {
                return (BindingSource.Query, queryAttribute.Name ?? parameter.Name ?? string.Empty);
            }
            else if (attributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
            {
                return (BindingSource.Header, headerAttribute.Name ?? parameter.Name ?? string.Empty);
            }
            else if (parameter.CustomAttributes.Any(a => typeof(IFromBodyMetadata).IsAssignableFrom(a.AttributeType)))
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty);
            }
            else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)) ||
                     parameter.ParameterType == typeof(HttpContext) ||
                     parameter.ParameterType == typeof(CancellationToken) ||
                     parameter.ParameterType.IsInterface)
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty);
            }
            else if (parameter.ParameterType == typeof(string) || RequestDelegateFactoryUtilities.HasTryParseMethod(parameter))
            {
                // TODO: Look at the pattern and infer whether it's really the path or query.
                // This cannot be done by RequestDelegateFactory currently because of the layering, but could be done here.
                return (BindingSource.PathOrQuery, parameter.Name ?? string.Empty);
            }
            else
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty);
            }
        }

        private static ApiResponseType? CreateApiResponseType(Type responseType)
        {
            if (typeof(IResult).IsAssignableFrom(responseType))
            {
                // Can't determine anything about IResults yet. IResult<T> could help here.
                // REVIEW: Is there any value in returning an ApiResponseType with StatusCode = 200 and that's it?
                return null;
            }
            else if (responseType == typeof(void))
            {
                return new ApiResponseType
                {
                    ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(typeof(void))),
                    StatusCode = 200,
                };
            }
            else if (responseType == typeof(string))
            {
                // This uses HttpResponse.WriteAsync(string) method which doesn't set a content type. It could be anything,
                // but I think "text/plain" is a reasonable assumption.
                return new ApiResponseType
                {
                    ApiResponseFormats = { new ApiResponseFormat { MediaType = "text/plain" } },
                    ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(typeof(string))),
                    StatusCode = 200,
                };
            }
            else
            {
                // Everything else is written using HttpResponse.WriteAsJsonAsync<TValue>(T).
                return new ApiResponseType
                {
                    ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                    ModelMetadata = new EndpointMethodInfoModelMetadata(ModelMetadataIdentity.ForType(responseType)),
                    StatusCode = 200,
                };
            }
        }
    }
}
