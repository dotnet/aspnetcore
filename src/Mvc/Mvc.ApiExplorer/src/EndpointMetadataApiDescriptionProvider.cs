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
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

internal sealed class EndpointMetadataApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProviderIsService? _serviceProviderIsService;
    private readonly ParameterPolicyFactory _parameterPolicyFactory;

    // Executes before MVC's DefaultApiDescriptionProvider and GrpcJsonTranscodingDescriptionProvider for no particular reason.
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
        // Keep in sync with EndpointRouteBuilderExtensions.cs
        static bool ShouldDisableInferredBody(string method)
        {
            // GET, DELETE, HEAD, CONNECT, TRACE, and OPTIONS normally do not contain bodies
            return method.Equals(HttpMethods.Get, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Delete, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Head, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Options, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Trace, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Connect, StringComparison.Ordinal);
        }

        foreach (var endpoint in _endpointDataSource.Endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint &&
                routeEndpoint.Metadata.GetMetadata<MethodInfo>() is { } methodInfo &&
                routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is { } httpMethodMetadata &&
                routeEndpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>() is null or { ExcludeFromDescription: false })
            {
                // We need to detect if any of the methods allow inferred body
                var disableInferredBody = httpMethodMetadata.HttpMethods.Any(ShouldDisableInferredBody);

                // REVIEW: Should we add an ApiDescription for endpoints without IHttpMethodMetadata? Swagger doesn't handle
                // a null HttpMethod even though it's nullable on ApiDescription, so we'd need to define "default" HTTP methods.
                // In practice, the Delegate will be called for any HTTP method if there is no IHttpMethodMetadata.
                foreach (var httpMethod in httpMethodMetadata.HttpMethods)
                {
                    context.Results.Add(CreateApiDescription(routeEndpoint, httpMethod, methodInfo, disableInferredBody));
                }
            }
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    private ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, string httpMethod, MethodInfo methodInfo, bool disableInferredBody)
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
            controllerName = _environment.ApplicationName ?? string.Empty;
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
        var parameters = routeEndpoint.Metadata.GetOrderedMetadata<IParameterBindingMetadata>();

        foreach (var parameter in parameters)
        {
            var parameterDescription = CreateApiParameterDescription(parameter, routeEndpoint, disableInferredBody);

            if (parameterDescription is { })
            {
                apiDescription.ParameterDescriptions.Add(parameterDescription);

                hasBodyOrFormFileParameter |=
                    parameterDescription.Source == BindingSource.Body ||
                    parameterDescription.Source == BindingSource.FormFile;
            }
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

    private ApiParameterDescription? CreateApiParameterDescription(IParameterBindingMetadata parameter, RouteEndpoint routeEndpoint, bool disableInferredBody)
    {
        var pattern = routeEndpoint.RoutePattern;
        var (source, name, _, paramType) = GetBindingSourceAndName(parameter, routeEndpoint, disableInferredBody);

        // Services are ignored because they are not request parameters.
        if (source == BindingSource.Services)
        {
            return null;
        }

        // Use the optionality status determined by the code generation layer which accounts for
        // nullability, default values, and the whether or not `[FromBody(AllowEmpty = true)]`.
        var isOptional = parameter.IsOptional;
        var parameterDescriptor = CreateParameterDescriptor(parameter.ParameterInfo, pattern);
        var routeInfo = CreateParameterRouteInfo(pattern, parameter.ParameterInfo, isOptional);

        return new ApiParameterDescription
        {
            Name = name,
            ModelMetadata = CreateModelMetadata(paramType),
            Source = source,
            DefaultValue = parameter.ParameterInfo.DefaultValue,
            Type = parameter.ParameterInfo.ParameterType,
            IsRequired = !isOptional,
            ParameterDescriptor = parameterDescriptor,
            RouteInfo = routeInfo
        };
    }

    private static ParameterDescriptor CreateParameterDescriptor(ParameterInfo parameter, RoutePattern pattern)
    {
        var parameterName = parameter.Name ?? string.Empty;
        var name = pattern.GetParameter(parameterName)?.Name ?? parameterName;
        return new EndpointParameterDescriptor
        {
            Name = name,
            ParameterInfo = parameter,
            ParameterType = parameter.ParameterType,
        };
    }

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
    private (BindingSource, string, bool, Type) GetBindingSourceAndName(IParameterBindingMetadata parameter, RouteEndpoint routeEndpoint, bool disableInferredBody)
    {
        var pattern = routeEndpoint.RoutePattern;
        var attributes = parameter.ParameterInfo.GetCustomAttributes();
        var parameterType = parameter.ParameterInfo.ParameterType;
        if (attributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
        {
            var parameterName = parameter.Name ?? string.Empty;
            var name = pattern.GetParameter(parameterName)?.Name ?? parameterName;
            return (BindingSource.Path, routeAttribute.Name ?? name, false, parameterType);
        }
        else if (attributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            return (BindingSource.Query, queryAttribute.Name ?? parameter.Name ?? string.Empty, false, parameterType);
        }
        else if (attributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            return (BindingSource.Header, headerAttribute.Name ?? parameter.Name ?? string.Empty, false, parameterType);
        }
        else if (attributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } fromBodyAttribute)
        {
            return (BindingSource.Body, parameter.Name ?? string.Empty, fromBodyAttribute.AllowEmpty, parameterType);
        }
        else if (attributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } fromFormAttribute)
        {
            return (BindingSource.FormFile, fromFormAttribute.Name ?? parameter.Name ?? string.Empty, false, parameterType);
        }
        else if (parameter.ParameterInfo.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType) || typeof(FromKeyedServicesAttribute) == a.AttributeType) ||
                 parameterType == typeof(HttpContext) ||
                 parameterType == typeof(HttpRequest) ||
                 parameterType == typeof(HttpResponse) ||
                 parameterType == typeof(ClaimsPrincipal) ||
                 parameterType == typeof(CancellationToken) ||
                 parameter.HasBindAsync ||
                 _serviceProviderIsService?.IsService(parameterType) == true)
        {
            return (BindingSource.Services, parameter.Name ?? string.Empty, false, parameterType);
        }
        else if (parameterType == typeof(string) || (!parameterType.IsArray && parameterType != typeof(StringValues) && parameter.HasTryParse))
        {
            // complex types will display as strings since they use custom parsing via TryParse on a string
            var displayType = EndpointModelMetadata.GetDisplayType(parameterType);

            // Path vs query cannot be determined by RequestDelegateFactory at startup currently because of the layering, but can be done here.
            if (parameter.Name is { } name && pattern.GetParameter(name) is { } routeParam)
            {
                return (BindingSource.Path, routeParam.Name, false, displayType);
            }
            else
            {
                return (BindingSource.Query, parameter.Name ?? string.Empty, false, displayType);
            }
        }
        else if (parameterType == typeof(IFormFile) || parameterType == typeof(IFormFileCollection))
        {
            return (BindingSource.FormFile, parameter.Name ?? string.Empty, false, parameterType);
        }
        else if (disableInferredBody && (
                 parameterType == typeof(string[]) ||
                 parameterType == typeof(StringValues) ||
                 (parameterType.IsArray && parameter.HasTryParse)))
        {
            return (BindingSource.Query, parameter.Name ?? string.Empty, false, parameterType);
        }
        else
        {
            return (BindingSource.Body, parameter.Name ?? string.Empty, false, parameterType);
        }
    }

    private static void AddSupportedResponseTypes(
        IList<ApiResponseType> supportedResponseTypes,
        Type returnType,
        EndpointMetadataCollection endpointMetadata)
    {
        var responseType = returnType;

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
            responseProviderMetadata, responseType, defaultErrorType, contentTypes, out var errorSetByDefault);
        var producesResponseMetadataTypes = ApiResponseTypeProvider.ReadResponseMetadata(producesResponseMetadata, responseType);

        // We favor types added via the extension methods (which implements IProducesResponseTypeMetadata)
        // over those that are added via attributes.
        var responseMetadataTypes = producesResponseMetadataTypes.Values.Concat(responseProviderMetadataTypes.Values);

        if (responseMetadataTypes.Any())
        {
            foreach (var apiResponseType in responseMetadataTypes)
            {
                // In some context, a typeof(void) return means that no response type was specified by the metadata. This can happen
                // if a user applied a [ProducesResponseType] attribute without a default type parameter. In this case, we should use the
                // response type inferred from the return type of the handler. For minimal API scenarios, where `typeof(void)` can be inferred
                // by the framework for handlers that return awaitables, we will only treat `typeof(void)` as a null type that should fall back to the
                // inference logic if it has been set as the default error type to retain back-compat.
                if (apiResponseType.Type is null || (apiResponseType.Type == typeof(void) && errorSetByDefault))
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
