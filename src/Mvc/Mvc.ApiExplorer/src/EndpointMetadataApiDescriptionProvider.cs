// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal class EndpointMetadataApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly EndpointDataSource _endpointDataSource;
        private readonly IServiceProviderIsService? _serviceProviderIsService;

        // Executes before MVC's DefaultApiDescriptionProvider and GrpcHttpApiDescriptionProvider for no particular reason :D
        public int Order => -1100;

        public EndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource)
            : this(endpointDataSource, null)
        {
        }

        public EndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource, IServiceProviderIsService? serviceProviderIsService)
        {
            _endpointDataSource = endpointDataSource;
            _serviceProviderIsService = serviceProviderIsService;
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
                        context.Results.Add(CreateApiDescription(routeEndpoint, httpMethod, methodInfo));
                    }
                }
            }
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        private ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, string httpMethod, MethodInfo methodInfo)
        {
            // Swagger uses the "controller" name to group endpoints together.
            // For now, put all methods defined the same declaring type together.
            string controllerName;

            if (methodInfo.DeclaringType is not null && !IsCompilerGenerated(methodInfo.DeclaringType))
            {
                controllerName = methodInfo.DeclaringType.Name;
            }
            else
            {
                // If the declaring type is null or compiler-generated (e.g. lambdas),
                // group the methods under a "Map" controller.
                controllerName = "Map";
            }

            var apiDescription = new ApiDescription
            {
                HttpMethod = httpMethod,
                RelativePath = routeEndpoint.RoutePattern.RawText?.TrimStart('/'),
                ActionDescriptor = new ActionDescriptor
                {
                    RouteValues =
                    {
                        ["controller"] = controllerName,
                    },
                },
            };

            var hasJsonBody = false;

            foreach (var parameter in methodInfo.GetParameters())
            {
                var parameterDescription = CreateApiParameterDescription(parameter, routeEndpoint.RoutePattern);

                if (parameterDescription.Source == BindingSource.Body)
                {
                    hasJsonBody = true;
                }

                apiDescription.ParameterDescriptions.Add(parameterDescription);
            }

            AddSupportedRequestFormats(apiDescription.SupportedRequestFormats, hasJsonBody, routeEndpoint.Metadata);
            AddSupportedResponseTypes(apiDescription.SupportedResponseTypes, methodInfo.ReturnType, routeEndpoint.Metadata);

            return apiDescription;
        }

        private ApiParameterDescription CreateApiParameterDescription(ParameterInfo parameter, RoutePattern pattern)
        {
            var (source, name) = GetBindingSourceAndName(parameter, pattern);

            return new ApiParameterDescription
            {
                Name = name,
                ModelMetadata = CreateModelMetadata(parameter.ParameterType),
                Source = source,
                DefaultValue = parameter.DefaultValue,
                Type = parameter.ParameterType,
            };
        }

        // TODO: Share more of this logic with RequestDelegateFactory.CreateArgument(...) using RequestDelegateFactoryUtilities
        // which is shared source.
        private (BindingSource, string) GetBindingSourceAndName(ParameterInfo parameter, RoutePattern pattern)
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
                     _serviceProviderIsService?.IsService(parameter.ParameterType) == true)
            {
                return (BindingSource.Services, parameter.Name ?? string.Empty);
            }
            else if (parameter.ParameterType == typeof(string) || RequestDelegateFactoryUtilities.HasTryParseMethod(parameter))
            {
                // Path vs query cannot be determined by RequestDelegateFactory at startup currently because of the layering, but can be done here.
                if (parameter.Name is { } name && pattern.GetParameter(name) is not null)
                {
                    return (BindingSource.Path, name);
                }
                else
                {
                    return (BindingSource.Query, parameter.Name ?? string.Empty);
                }
            }
            else
            {
                return (BindingSource.Body, parameter.Name ?? string.Empty);
            }
        }

        private static void AddSupportedRequestFormats(
            IList<ApiRequestFormat> supportedRequestFormats,
            bool hasJsonBody,
            EndpointMetadataCollection endpointMetadata)
        {
            var requestMetadata = endpointMetadata.GetOrderedMetadata<IApiRequestMetadataProvider>();
            var declaredContentTypes = DefaultApiDescriptionProvider.GetDeclaredContentTypes(requestMetadata);

            if (declaredContentTypes.Count > 0)
            {
                foreach (var contentType in declaredContentTypes)
                {
                    supportedRequestFormats.Add(new ApiRequestFormat
                    {
                        MediaType = contentType,
                    });
                }
            }
            else if (hasJsonBody)
            {
                supportedRequestFormats.Add(new ApiRequestFormat
                {
                    MediaType = "application/json",
                });
            }
        }

        private static void AddSupportedResponseTypes(
            IList<ApiResponseType> supportedResponseTypes,
            Type returnType,
            EndpointMetadataCollection endpointMetadata)
        {
            var responseType = returnType;

            if (AwaitableInfo.IsTypeAwaitable(responseType, out var awaitableInfo))
            {
                responseType = awaitableInfo.ResultType;
            }

            // Can't determine anything about IResults yet that's not from extra metadata. IResult<T> could help here.
            if (typeof(IResult).IsAssignableFrom(responseType))
            {
                responseType = typeof(void);
            }

            var responseMetadata = endpointMetadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
            var errorMetadata = endpointMetadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
            var defaultErrorType = errorMetadata?.Type ?? typeof(void);
            var contentTypes = new MediaTypeCollection();

            var responseMetadataTypes = ApiResponseTypeProvider.ReadResponseMetadata(
                responseMetadata, responseType, defaultErrorType, contentTypes);

            if (responseMetadataTypes.Count > 0)
            {
                foreach (var apiResponseType in responseMetadataTypes)
                {
                    // void means no response type was specified by the metadata, so use whatever we inferred.
                    // ApiResponseTypeProvider should never return ApiResponseTypes with null Type, but it doesn't hurt to check.
                    if (apiResponseType.Type is null || apiResponseType.Type == typeof(void))
                    {
                        apiResponseType.Type = responseType;
                    }

                    apiResponseType.ModelMetadata = CreateModelMetadata(apiResponseType.Type);

                    if (contentTypes.Count > 0)
                    {
                        AddResponseContentTypes(apiResponseType.ApiResponseFormats, contentTypes);
                    }
                    else if (CreateDefaultApiResponseFormat(responseType) is { } defaultResponseFormat)
                    {
                        apiResponseType.ApiResponseFormats.Add(defaultResponseFormat);
                    }

                    supportedResponseTypes.Add(apiResponseType);
                }
            }
            else
            {
                // Set the default response type only when none has already been set explicitly with metadata.
                var defaultApiResponseType = CreateDefaultApiResponseType(responseType);

                if (contentTypes.Count > 0)
                {
                    // If metadata provided us with response formats, use that instead of the default.
                    defaultApiResponseType.ApiResponseFormats.Clear();
                    AddResponseContentTypes(defaultApiResponseType.ApiResponseFormats, contentTypes);
                }

                supportedResponseTypes.Add(defaultApiResponseType);
            }
        }

        private static ApiResponseType CreateDefaultApiResponseType(Type responseType)
        {
            var apiResponseType = new ApiResponseType
            {
                ModelMetadata = CreateModelMetadata(responseType),
                StatusCode = 200,
                Type = responseType,
            };

            if (CreateDefaultApiResponseFormat(responseType) is { } responseFormat)
            {
                apiResponseType.ApiResponseFormats.Add(responseFormat);
            }

            return apiResponseType;
        }

        private static ApiResponseFormat? CreateDefaultApiResponseFormat(Type responseType)
        {
            if (responseType == typeof(void))
            {
                return null;
            }
            else if (responseType == typeof(string))
            {
                // This uses HttpResponse.WriteAsync(string) method which doesn't set a content type. It could be anything,
                // but I think "text/plain" is a reasonable assumption if nothing else is specified with metadata.
                return new ApiResponseFormat { MediaType = "text/plain" };
            }
            else
            {
                // Everything else is written using HttpResponse.WriteAsJsonAsync<TValue>(T).
                return new ApiResponseFormat { MediaType = "application/json" };
            }
        }

        private static EndpointModelMetadata CreateModelMetadata(Type type) =>
            new EndpointModelMetadata(ModelMetadataIdentity.ForType(type));

        private static void AddResponseContentTypes(IList<ApiResponseFormat> apiResponseFormats, IReadOnlyList<string> contentTypes)
        {
            foreach (var contentType in contentTypes)
            {
                apiResponseFormats.Add(new ApiResponseFormat
                {
                    MediaType = contentType,
                });
            }
        }

        // The CompilerGeneratedAttribute doesn't always get added so we also check if the type name starts with "<"
        // For example,w "<>c" is a "declaring" type the C# compiler will generate without the attribute for a top-level lambda
        // REVIEW: Is there a better way to do this?
        private static bool IsCompilerGenerated(Type type) =>
            Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) || type.Name.StartsWith('<');
    }
}
