// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Google.Api;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Grpc.Swagger.Internal;

internal sealed class GrpcJsonTranscodingDescriptionProvider : IApiDescriptionProvider
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly DescriptorRegistry _descriptorRegistry;

    public GrpcJsonTranscodingDescriptionProvider(EndpointDataSource endpointDataSource, DescriptorRegistry descriptorRegistry)
    {
        _endpointDataSource = endpointDataSource;
        _descriptorRegistry = descriptorRegistry;
    }

    // Executes after ASP.NET Core
    public int Order => -900;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        var endpoints = _endpointDataSource.Endpoints;

        foreach (var endpoint in endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint)
            {
                var grpcMetadata = endpoint.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>();

                if (grpcMetadata != null)
                {
                    var httpRule = grpcMetadata.HttpRule;
                    var methodDescriptor = grpcMetadata.MethodDescriptor;

                    if (ServiceDescriptorHelpers.TryResolvePattern(grpcMetadata.HttpRule, out var pattern, out var verb))
                    {
                        var apiDescription = CreateApiDescription(routeEndpoint, httpRule, methodDescriptor, pattern, verb);
                        context.Results.Add(apiDescription);

                        _descriptorRegistry.RegisterFileDescriptor(grpcMetadata.MethodDescriptor.File);
                    }
                }
            }
        }
    }

    private static ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, HttpRule httpRule, MethodDescriptor methodDescriptor, string pattern, string verb)
    {
        var apiDescription = new ApiDescription();
        apiDescription.HttpMethod = verb;
        apiDescription.ActionDescriptor = new ActionDescriptor
        {
            RouteValues = new Dictionary<string, string?>
            {
                // Swagger uses this to group endpoints together.
                // Group methods together using the service name.
                ["controller"] = methodDescriptor.Service.Name
            },
            EndpointMetadata = routeEndpoint.Metadata.ToList()
        };
        apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });

        var responseBodyDescriptor = ServiceDescriptorHelpers.ResolveResponseBodyDescriptor(httpRule.ResponseBody, methodDescriptor);
        var responseType = responseBodyDescriptor != null ? MessageDescriptorHelpers.ResolveFieldType(responseBodyDescriptor) : methodDescriptor.OutputType.ClrType;
        apiDescription.SupportedResponseTypes.Add(new ApiResponseType
        {
            ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
            ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(responseType)),
            StatusCode = 200
        });
        apiDescription.SupportedResponseTypes.Add(new ApiResponseType
        {
            ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
            ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(typeof(Google.Rpc.Status))),
            IsDefaultResponse = true
        });
        var explorerSettings = routeEndpoint.Metadata.GetMetadata<ApiExplorerSettingsAttribute>();
        if (explorerSettings != null)
        {
            apiDescription.GroupName = explorerSettings.GroupName;
        }

        var methodMetadata = routeEndpoint.Metadata.GetMetadata<GrpcMethodMetadata>()!;
        var httpRoutePattern = HttpRoutePattern.Parse(pattern);
        var routeParameters = ServiceDescriptorHelpers.ResolveRouteParameterDescriptors(httpRoutePattern.Variables, methodDescriptor.InputType);

        apiDescription.RelativePath = ResolvePath(httpRoutePattern, routeParameters);

        foreach (var routeParameter in routeParameters)
        {
            var field = routeParameter.Value.DescriptorsPath.Last();
            var parameterName = ServiceDescriptorHelpers.FormatUnderscoreName(field.Name, pascalCase: true, preservePeriod: false);
            var propertyInfo = field.ContainingType.ClrType.GetProperty(parameterName);

            // If from a property, create model as property to get its XML comments.
            var identity = propertyInfo != null
                ? ModelMetadataIdentity.ForProperty(propertyInfo, MessageDescriptorHelpers.ResolveFieldType(field), field.ContainingType.ClrType)
                : ModelMetadataIdentity.ForType(MessageDescriptorHelpers.ResolveFieldType(field));

            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
            {
                Name = routeParameter.Value.JsonPath,
                ModelMetadata = new GrpcModelMetadata(identity),
                Source = BindingSource.Path,
                DefaultValue = string.Empty
            });
        }

        var bodyDescriptor = ServiceDescriptorHelpers.ResolveBodyDescriptor(httpRule.Body, methodMetadata.ServiceType, methodDescriptor);
        if (bodyDescriptor != null)
        {
            // If from a property, create model as property to get its XML comments.
            var identity = bodyDescriptor.PropertyInfo != null
                ? ModelMetadataIdentity.ForProperty(bodyDescriptor.PropertyInfo, bodyDescriptor.PropertyInfo.PropertyType, bodyDescriptor.PropertyInfo.DeclaringType!)
                : ModelMetadataIdentity.ForType(bodyDescriptor.Descriptor.ClrType);

            // Or if from a parameter, create model as parameter to get its XML comments.
            var parameterDescriptor = bodyDescriptor.ParameterInfo != null
                ? new ControllerParameterDescriptor { ParameterInfo = bodyDescriptor.ParameterInfo }
                : null;

            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
            {
                Name = "Input",
                ModelMetadata = new GrpcModelMetadata(identity),
                Source = BindingSource.Body,
                ParameterDescriptor = parameterDescriptor!
            });
        }

        var queryParameters = ServiceDescriptorHelpers.ResolveQueryParameterDescriptors(routeParameters, methodDescriptor, bodyDescriptor?.Descriptor, bodyDescriptor?.FieldDescriptor);
        foreach (var queryDescription in queryParameters)
        {
            var field = queryDescription.Value;
            var propertyInfo = field.ContainingType.ClrType.GetProperty(field.PropertyName);

            // If from a property, create model as property to get its XML comments.
            var identity = propertyInfo != null
                ? ModelMetadataIdentity.ForProperty(propertyInfo, MessageDescriptorHelpers.ResolveFieldType(field), field.ContainingType.ClrType)
                : ModelMetadataIdentity.ForType(MessageDescriptorHelpers.ResolveFieldType(field));

            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
            {
                Name = queryDescription.Key,
                ModelMetadata = new GrpcModelMetadata(identity),
                Source = BindingSource.Query,
                DefaultValue = string.Empty
            });
        }

        return apiDescription;
    }

    private static string ResolvePath(HttpRoutePattern httpRoutePattern, Dictionary<string, RouteParameter> routeParameters)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < httpRoutePattern.Segments.Count; i++)
        {
            if (sb.Length > 0)
            {
                sb.Append('/');
            }
            var routeParameter = routeParameters.SingleOrDefault(kvp => kvp.Value.RouteVariable.StartSegment == i).Value;
            if (routeParameter != null)
            {
                sb.Append('{');
                sb.Append(routeParameter.JsonPath);
                sb.Append('}');

                // Skip segments if variable is multiple segment.
                i = routeParameter.RouteVariable.EndSegment - 1;
            }
            else
            {
                sb.Append(httpRoutePattern.Segments[i]);
            }
        }
        if (httpRoutePattern.Verb != null)
        {
            sb.Append(':');
            sb.Append(httpRoutePattern.Verb);
        }
        return sb.ToString();
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        // no-op
    }
}
