// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

internal class EndpointMetadataApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProviderIsService? _serviceProviderIsService;
    private readonly ParameterBindingMethodCache ParameterBindingMethodCache = new();
    private readonly ParameterPolicyFactory _parameterPolicyFactory;

    // Executes before MVC's DefaultApiDescriptionProvider and GrpcHttpApiDescriptionProvider for no particular reason.
    public int Order => -1100;

    public EndpointMetadataApiDescriptionProvider(
        EndpointDataSource endpointDataSource,
        IHostEnvironment environment,
        ParameterPolicyFactory parameterPolicyFactory,
        IServiceProviderIsService? serviceProviderIsService)
    {
        _endpointDataSource = endpointDataSource;
        _environment = environment;
        _serviceProviderIsService = serviceProviderIsService;
        _parameterPolicyFactory = parameterPolicyFactory;
    }

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        foreach (var endpoint in _endpointDataSource.Endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint &&
                routeEndpoint.Metadata.GetMetadata<MethodInfo>() is { } methodInfo &&
                routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is { } httpMethodMetadata &&
                routeEndpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>() is null or { ExcludeFromDescription: false })
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
        // Swashbuckle uses the "controller" name to group endpoints together.
        // For now, put all methods defined the same declaring type together.
        string controllerName;

        if (methodInfo.DeclaringType is not null && !TypeHelper.IsCompilerGeneratedType(methodInfo.DeclaringType))
        {
            controllerName = methodInfo.DeclaringType.Name;
        }
        else
        {
            // If the declaring type is null or compiler-generated (e.g. lambdas),
            // group the methods under the application name.
            controllerName = _environment.ApplicationName;
        }

        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod,
            GroupName = routeEndpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName,
            RelativePath = routeEndpoint.RoutePattern.RawText?.TrimStart('/'),
            ActionDescriptor = new ActionDescriptor
            {
                DisplayName = routeEndpoint.DisplayName,
                RouteValues =
                {
                    ["controller"] = controllerName,
                },
            },
        };

        var hasBodyOrFormFileParameter = false;

        foreach (var parameter in methodInfo.GetParameters())
        {
            var parameterDescription = CreateApiParameterDescription(parameter, routeEndpoint.RoutePattern);

            if (parameterDescription is null)
            {
                continue;
            }

            apiDescription.ParameterDescriptions.Add(parameterDescription);

            hasBodyOrFormFileParameter |=
                parameterDescription.Source == BindingSource.Body ||
                parameterDescription.Source == BindingSource.FormFile;
        }

        // Get IAcceptsMetadata.
        var acceptsMetadata = routeEndpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        if (acceptsMetadata is not null)
        {
            // Add a default body parameter if there was no explicitly defined parameter associated with
            // either the body or a form and the user explicity defined some metadata describing the
            // content types the endpoint consumes (such as Accepts<TRequest>(...) or [Consumes(...)]).
            if (!hasBodyOrFormFileParameter)
            {
                var acceptsRequestType = acceptsMetadata.RequestType;
                var isOptional = acceptsMetadata.IsOptional;
                var parameterDescription = new ApiParameterDescription
                {
                    Name = acceptsRequestType is not null ? acceptsRequestType.Name : typeof(void).Name,
                    ModelMetadata = CreateModelMetadata(acceptsRequestType ?? typeof(void)),
                    Source = BindingSource.Body,
                    Type = acceptsRequestType ?? typeof(void),
                    IsRequired = !isOptional,
                };
                apiDescription.ParameterDescriptions.Add(parameterDescription);
            }

            var supportedRequestFormats = apiDescription.SupportedRequestFormats;

            foreach (var contentType in acceptsMetadata.ContentTypes)
            {
                supportedRequestFormats.Add(new ApiRequestFormat
                {
                    MediaType = contentType
                });
            }
        }

        AddSupportedResponseTypes(apiDescription.SupportedResponseTypes, methodInfo.ReturnType, routeEndpoint.Metadata);
        AddActionDescriptorEndpointMetadata(apiDescription.ActionDescriptor, routeEndpoint.Metadata);

        return apiDescription;
    }

    private ApiParameterDescription? CreateApiParameterDescription(ParameterInfo parameter, RoutePattern pattern)
    {
        var (source, name, allowEmpty, paramType) = GetBindingSourceAndName(parameter, pattern);

        // Services are ignored because they are not request parameters.
        if (source == BindingSource.Services)
        {
            return null;
        }

        // Determine the "requiredness" based on nullability, default value or if allowEmpty is set
        var nullabilityContext = new NullabilityInfoContext();
        var nullability = nullabilityContext.Create(parameter);
        var isOptional = parameter.HasDefaultValue || nullability.ReadState != NullabilityState.NotNull || allowEmpty;
        var parameterDescriptor = CreateParameterDescriptor(parameter);
        var routeInfo = CreateParameterRouteInfo(pattern, parameter, isOptional);

        return new ApiParameterDescription
        {
            Name = name,
            ModelMetadata = CreateModelMetadata(paramType),
            Source = source,
            DefaultValue = parameter.DefaultValue,
            Type = parameter.ParameterType,
            IsRequired = !isOptional,
            ParameterDescriptor = parameterDescriptor,
            RouteInfo = routeInfo
        };
    }

    private static ParameterDescriptor CreateParameterDescriptor(ParameterInfo parameter)
        => new EndpointParameterDescriptor
        {
            Name = parameter.Name ?? string.Empty,
            ParameterInfo = parameter,
            ParameterType = parameter.ParameterType,
        };

    private ApiParameterRouteInfo? CreateParameterRouteInfo(RoutePattern pattern, ParameterInfo parameter, bool isOptional)
    {
        if (parameter.Name is null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameter.ParameterType}' without a name. Parameters must have a name.");
        }

        // Only produce a `RouteInfo` property for parameters that are defined in the route template
        if (pattern.GetParameter(parameter.Name) is not RoutePatternParameterPart parameterPart)
        {
            return null;
        }

        var constraints = new List<IRouteConstraint>();

        if (pattern.ParameterPolicies.TryGetValue(parameter.Name, out var parameterPolicyReferences))
        {
            foreach (var parameterPolicyReference in parameterPolicyReferences)
            {
                var policy = _parameterPolicyFactory.Create(parameterPart, parameterPolicyReference);
                if (policy is IRouteConstraint generatedConstraint)
                {
                    constraints.Add(generatedConstraint);
                }
            }
        }

        return new ApiParameterRouteInfo()
        {
            Constraints = constraints.AsReadOnly(),
            DefaultValue = parameter.DefaultValue,
            IsOptional = isOptional
        };
    }

    // TODO: Share more of this logic with RequestDelegateFactory.CreateArgument(...) using RequestDelegateFactoryUtilities
    // which is shared source.
    private (BindingSource, string, bool, Type) GetBindingSourceAndName(ParameterInfo parameter, RoutePattern pattern)
    {
        var attributes = parameter.GetCustomAttributes();

        if (attributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
        {
            return (BindingSource.Path, routeAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else if (attributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            return (BindingSource.Query, queryAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else if (attributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            return (BindingSource.Header, headerAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else if (attributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } fromBodyAttribute)
        {
            return (BindingSource.Body, parameter.Name ?? string.Empty, fromBodyAttribute.AllowEmpty, parameter.ParameterType);
        }
        else if (attributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } fromFormAttribute)
        {
            return (BindingSource.FormFile, fromFormAttribute.Name ?? parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)) ||
                 parameter.ParameterType == typeof(HttpContext) ||
                 parameter.ParameterType == typeof(HttpRequest) ||
                 parameter.ParameterType == typeof(HttpResponse) ||
                 parameter.ParameterType == typeof(ClaimsPrincipal) ||
                 parameter.ParameterType == typeof(CancellationToken) ||
                 ParameterBindingMethodCache.HasBindAsyncMethod(parameter) ||
                 _serviceProviderIsService?.IsService(parameter.ParameterType) == true)
        {
            return (BindingSource.Services, parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else if (parameter.ParameterType == typeof(string) || ParameterBindingMethodCache.HasTryParseMethod(parameter))
        {
            // complex types will display as strings since they use custom parsing via TryParse on a string
            var displayType = !parameter.ParameterType.IsPrimitive && Nullable.GetUnderlyingType(parameter.ParameterType)?.IsPrimitive != true
                ? typeof(string) : parameter.ParameterType;
            // Path vs query cannot be determined by RequestDelegateFactory at startup currently because of the layering, but can be done here.
            if (parameter.Name is { } name && pattern.GetParameter(name) is not null)
            {
                return (BindingSource.Path, name, false, displayType);
            }
            else
            {
                return (BindingSource.Query, parameter.Name ?? string.Empty, false, displayType);
            }
        }
        else if (parameter.ParameterType == typeof(IFormFile) || parameter.ParameterType == typeof(IFormFileCollection))
        {
            return (BindingSource.FormFile, parameter.Name ?? string.Empty, false, parameter.ParameterType);
        }
        else
        {
            return (BindingSource.Body, parameter.Name ?? string.Empty, false, parameter.ParameterType);
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

        // We support attributes (which implement the IApiResponseMetadataProvider) interface
        // and types added via the extension methods (which implement IProducesResponseTypeMetadata).
        var responseProviderMetadata = endpointMetadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
        var producesResponseMetadata = endpointMetadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        var errorMetadata = endpointMetadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
        var defaultErrorType = errorMetadata?.Type ?? typeof(void);
        var contentTypes = new MediaTypeCollection();

        var responseProviderMetadataTypes = ApiResponseTypeProvider.ReadResponseMetadata(
            responseProviderMetadata, responseType, defaultErrorType, contentTypes);
        var producesResponseMetadataTypes = ReadResponseMetadata(producesResponseMetadata, responseType);

        // We favor types added via the extension methods (which implements IProducesResponseTypeMetadata)
        // over those that are added via attributes.
        var responseMetadataTypes = producesResponseMetadataTypes.Values.Concat(responseProviderMetadataTypes);

        if (responseMetadataTypes.Any())
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
                // Only set the default response type if it hasn't already been set via a
                // ProducesResponseTypeAttribute.
                else if (apiResponseType.ApiResponseFormats.Count == 0 && CreateDefaultApiResponseFormat(apiResponseType.Type) is { } defaultResponseFormat)
                {
                    apiResponseType.ApiResponseFormats.Add(defaultResponseFormat);
                }

                if (!supportedResponseTypes.Any(existingResponseType => existingResponseType.StatusCode == apiResponseType.StatusCode))
                {
                    supportedResponseTypes.Add(apiResponseType);
                }
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

    private static Dictionary<int, ApiResponseType> ReadResponseMetadata(
        IReadOnlyList<IProducesResponseTypeMetadata> responseMetadata,
        Type? type)
    {
        var results = new Dictionary<int, ApiResponseType>();

        foreach (var metadata in responseMetadata)
        {
            var statusCode = metadata.StatusCode;

            var apiResponseType = new ApiResponseType
            {
                Type = metadata.Type,
                StatusCode = statusCode,
            };

            if (apiResponseType.Type == typeof(void))
            {
                if (type != null && (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created))
                {
                    // Allow setting the response type from the return type of the method if it has
                    // not been set explicitly by the method.
                    apiResponseType.Type = type;
                }
            }

            var attributeContentTypes = new MediaTypeCollection();
            if (metadata.ContentTypes != null)
            {
                foreach (var contentType in metadata.ContentTypes)
                {
                    attributeContentTypes.Add(contentType);
                }
            }
            ApiResponseTypeProvider.CalculateResponseFormatForType(apiResponseType, attributeContentTypes, responseTypeMetadataProviders: null, modelMetadataProvider: null);

            if (apiResponseType.Type != null)
            {
                results[apiResponseType.StatusCode] = apiResponseType;
            }
        }

        return results;
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
        new(ModelMetadataIdentity.ForType(type));

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

    private static void AddActionDescriptorEndpointMetadata(
        ActionDescriptor actionDescriptor,
        EndpointMetadataCollection endpointMetadata)
    {
        if (endpointMetadata.Count > 0)
        {
            // ActionDescriptor.EndpointMetadata is an empty array by
            // default so need to add the metadata into a new list.
            actionDescriptor.EndpointMetadata = new List<object>(endpointMetadata);
        }
    }
}
