// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

internal sealed class ApiResponseTypeProvider
{
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly IActionResultTypeMapper _mapper;
    private readonly MvcOptions _mvcOptions;

    public ApiResponseTypeProvider(
        IModelMetadataProvider modelMetadataProvider,
        IActionResultTypeMapper mapper,
        MvcOptions mvcOptions)
    {
        _modelMetadataProvider = modelMetadataProvider;
        _mapper = mapper;
        _mvcOptions = mvcOptions;
    }

    public ICollection<ApiResponseType> GetApiResponseTypes(ControllerActionDescriptor action)
    {
        // We only provide response info if we can figure out a type that is a user-data type.
        // Void /Task object/IActionResult will result in no data.
        var declaredReturnType = GetDeclaredReturnType(action);

        var runtimeReturnType = GetRuntimeReturnType(declaredReturnType);

        var responseMetadataAttributes = GetResponseMetadataAttributes(action);
        if (!HasSignificantMetadataProvider(responseMetadataAttributes) &&
            action.Properties.TryGetValue(typeof(ApiConventionResult), out var result))
        {
            // Action does not have any conventions. Use conventions on it if present.
            var apiConventionResult = (ApiConventionResult)result!;
            responseMetadataAttributes.AddRange(apiConventionResult.ResponseMetadataProviders);
        }

        var defaultErrorType = typeof(void);
        if (action.Properties.TryGetValue(typeof(ProducesErrorResponseTypeAttribute), out result))
        {
            defaultErrorType = ((ProducesErrorResponseTypeAttribute)result!).Type;
        }

        var producesResponseMetadata = action.EndpointMetadata.OfType<IProducesResponseTypeMetadata>().ToList();
        var apiResponseTypes = GetApiResponseTypes(responseMetadataAttributes, producesResponseMetadata, runtimeReturnType, defaultErrorType);
        return apiResponseTypes;
    }

    private static List<IApiResponseMetadataProvider> GetResponseMetadataAttributes(ControllerActionDescriptor action)
    {
        if (action.FilterDescriptors == null)
        {
            return new List<IApiResponseMetadataProvider>();
        }

        // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
        // while searching for a filter that implements IApiResponseMetadataProvider.
        //
        // The workaround for that is to implement the metadata interface on the IFilterFactory.
        return action.FilterDescriptors
            .Select(fd => fd.Filter)
            .OfType<IApiResponseMetadataProvider>()
            .ToList();
    }

    private ICollection<ApiResponseType> GetApiResponseTypes(
       IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
       IReadOnlyList<IProducesResponseTypeMetadata> producesResponseMetadata,
       Type? type,
       Type defaultErrorType)
    {
        var contentTypes = new MediaTypeCollection();
        var responseTypeMetadataProviders = _mvcOptions.OutputFormatters.OfType<IApiResponseTypeMetadataProvider>();

        var responseTypes = ReadResponseMetadata(
            producesResponseMetadata,
            type,
            responseTypeMetadataProviders,
            _modelMetadataProvider);

        // Read response metadata from providers and
        // overwrite responseTypes from the metadata based
        // on the status code
        var responseTypesFromProvider = ReadResponseMetadata(
            responseMetadataAttributes,
            type,
            defaultErrorType,
            contentTypes,
            out var _,
            responseTypeMetadataProviders);

        foreach (var responseType in responseTypesFromProvider)
        {
            responseTypes[responseType.Key] = responseType.Value;
        }

        // Set the default status only when no status has already been set explicitly
        if (responseTypes.Count == 0 && type != null)
        {
            responseTypes.Add(StatusCodes.Status200OK, new ApiResponseType
            {
                StatusCode = StatusCodes.Status200OK,
                Type = type,
            });
        }

        if (contentTypes.Count == 0)
        {
            // None of the IApiResponseMetadataProvider specified a content type. This is common for actions that
            // specify one or more ProducesResponseType but no ProducesAttribute. In this case, formatters will participate in conneg
            // and respond to the incoming request.
            // Querying IApiResponseTypeMetadataProvider.GetSupportedContentTypes with "null" should retrieve all supported
            // content types that each formatter may respond in.
            contentTypes.Add((string)null!);
        }

        foreach (var apiResponse in responseTypes.Values)
        {
            CalculateResponseFormatForType(apiResponse, contentTypes, responseTypeMetadataProviders, _modelMetadataProvider);
        }

        return responseTypes.Values;
    }

    // Shared with EndpointMetadataApiDescriptionProvider
    internal static Dictionary<int, ApiResponseType> ReadResponseMetadata(
        IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
        Type? type,
        Type? defaultErrorType,
        MediaTypeCollection contentTypes,
        out bool errorSetByDefault,
        IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
        IModelMetadataProvider? modelMetadataProvider = null)
    {
        errorSetByDefault = false;
        var results = new Dictionary<int, ApiResponseType>();

        // Get the content type that the action explicitly set to support.
        // Walk through all 'filter' attributes in order, and allow each one to see or override
        // the results of the previous ones. This is similar to the execution path for content-negotiation.
        if (responseMetadataAttributes != null)
        {
            foreach (var metadataAttribute in responseMetadataAttributes)
            {
                // All ProducesXAttributes, except for ProducesResponseTypeAttribute do
                // not allow multiple instances on the same method/class/etc. For those
                // scenarios, the `SetContentTypes` method on the attribute continuously
                // clears out more general content types in favor of more specific ones
                // since we iterate through the attributes in order. For example, if a
                // Produces exists on both a controller and an action within the controller,
                // we favor the definition in the action. This is a semantic that does not
                // apply to ProducesResponseType, which allows multiple instances on an target.
                if (metadataAttribute is not ProducesResponseTypeAttribute)
                {
                    metadataAttribute.SetContentTypes(contentTypes);
                }

                var statusCode = metadataAttribute.StatusCode;

                var apiResponseType = new ApiResponseType
                {
                    Type = metadataAttribute.Type,
                    StatusCode = statusCode,
                    IsDefaultResponse = metadataAttribute is IApiDefaultResponseMetadataProvider,
                };

                if (apiResponseType.Type == typeof(void))
                {
                    if (type != null && (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created))
                    {
                        // ProducesResponseTypeAttribute's constructor defaults to setting "Type" to void when no value is specified.
                        // In this event, use the action's return type for 200 or 201 status codes. This lets you decorate an action with a
                        // [ProducesResponseType(201)] instead of [ProducesResponseType(typeof(Person), 201] when typeof(Person) can be inferred
                        // from the return type.
                        apiResponseType.Type = type;
                    }
                    else if (IsClientError(statusCode))
                    {
                        // Determine whether or not the type was provided by the user. If so, favor it over the default
                        // error type for 4xx client errors if no response type is specified..
                        errorSetByDefault = metadataAttribute is ProducesResponseTypeAttribute { IsResponseTypeSetByDefault: true };
                        apiResponseType.Type = errorSetByDefault ? defaultErrorType : apiResponseType.Type;
                    }
                    else if (apiResponseType.IsDefaultResponse)
                    {
                        apiResponseType.Type = defaultErrorType;
                    }
                }

                // We special case the handling of ProcuesResponseTypeAttributes since
                // multiple ProducesResponseTypeAttributes are permitted on a single
                // action/controller/etc. In that scenario, instead of picking the most-specific
                // set of content types (like we do with the Produces attribute above) we process
                // the content types for each attribute independently.
                if (metadataAttribute is ProducesResponseTypeAttribute)
                {
                    var attributeContentTypes = new MediaTypeCollection();
                    metadataAttribute.SetContentTypes(attributeContentTypes);
                    CalculateResponseFormatForType(apiResponseType, attributeContentTypes, responseTypeMetadataProviders, modelMetadataProvider);
                }

                if (apiResponseType.Type != null)
                {
                    results[apiResponseType.StatusCode] = apiResponseType;
                }
            }
        }

        return results;
    }

    internal static Dictionary<int, ApiResponseType> ReadResponseMetadata(
        IReadOnlyList<IProducesResponseTypeMetadata> responseMetadata,
        Type? type,
        IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
        IModelMetadataProvider? modelMetadataProvider = null)
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

            if (apiResponseType.Type == null)
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

            CalculateResponseFormatForType(apiResponseType, attributeContentTypes, responseTypeMetadataProviders, modelMetadataProvider);

            if (apiResponseType.Type != null)
            {
                results[apiResponseType.StatusCode] = apiResponseType;
            }
        }

        return results;
    }

    // Shared with EndpointMetadataApiDescriptionProvider
    internal static void CalculateResponseFormatForType(ApiResponseType apiResponse, MediaTypeCollection declaredContentTypes, IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders, IModelMetadataProvider? modelMetadataProvider)
    {
        // If response formats have already been calculate for this type,
        // then exit early. This avoids populating the ApiResponseFormat for
        // types that have already been handled, specifically ProducesResponseTypes.
        if (apiResponse.ApiResponseFormats.Count > 0)
        {
            return;
        }

        // Given the content-types that were declared for this action, determine the formatters that support the content-type for the given
        // response type.
        // 1. Responses that do not specify an type do not have any associated content-type. This usually is meant for status-code only responses such
        // as return NotFound();
        // 2. When a type is specified, use GetSupportedContentTypes to expand wildcards and get the range of content-types formatters support.
        // 3. When no formatter supports the specified content-type, use the user specified value as is. This is useful in actions where the user
        // dictates the content-type.
        // e.g. [Produces("application/pdf")] Action() => FileStream("somefile.pdf", "application/pdf");
        var responseType = apiResponse.Type;
        if (responseType == null || responseType == typeof(void))
        {
            return;
        }

        apiResponse.ModelMetadata = modelMetadataProvider?.GetMetadataForType(responseType);

        foreach (var contentType in declaredContentTypes)
        {
            var isSupportedContentType = false;

            if (responseTypeMetadataProviders != null)
            {
                foreach (var responseTypeMetadataProvider in responseTypeMetadataProviders)
                {
                    var formatterSupportedContentTypes = responseTypeMetadataProvider.GetSupportedContentTypes(
                        contentType,
                        responseType);

                    if (formatterSupportedContentTypes == null)
                    {
                        continue;
                    }

                    isSupportedContentType = true;

                    foreach (var formatterSupportedContentType in formatterSupportedContentTypes)
                    {
                        apiResponse.ApiResponseFormats.Add(new ApiResponseFormat
                        {
                            Formatter = (IOutputFormatter)responseTypeMetadataProvider,
                            MediaType = formatterSupportedContentType,
                        });
                    }
                }
            }

            if (!isSupportedContentType && contentType != null)
            {
                // No output formatter was found that supports this content type. Add the user specified content type as-is to the result.
                apiResponse.ApiResponseFormats.Add(new ApiResponseFormat
                {
                    MediaType = contentType,
                });
            }
        }
    }

    private Type? GetDeclaredReturnType(ControllerActionDescriptor action)
    {
        var declaredReturnType = action.MethodInfo.ReturnType;
        if (declaredReturnType == typeof(void) ||
            declaredReturnType == typeof(Task) ||
            declaredReturnType == typeof(ValueTask))
        {
            return typeof(void);
        }

        // Unwrap the type if it's a Task<T>. The Task (non-generic) case was already handled.
        var unwrappedType = declaredReturnType;
        if (declaredReturnType.IsGenericType &&
            (declaredReturnType.GetGenericTypeDefinition() == typeof(Task<>) || declaredReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            unwrappedType = declaredReturnType.GetGenericArguments()[0];
        }

        // If the method is declared to return IActionResult, IResult or a derived class, that information
        // isn't valuable to the formatter.
        if (typeof(IActionResult).IsAssignableFrom(unwrappedType) ||
            typeof(IResult).IsAssignableFrom(unwrappedType))
        {
            return null;
        }

        // If we get here, the type should be a user-defined data type or an envelope type
        // like ActionResult<T>. The mapper service will unwrap envelopes.
        unwrappedType = _mapper.GetResultDataType(unwrappedType);
        return unwrappedType;
    }

    private static Type? GetRuntimeReturnType(Type? declaredReturnType)
    {
        // If we get here, then a filter didn't give us an answer, so we need to figure out if we
        // want to use the declared return type.
        //
        // We've already excluded Task, void, and IActionResult at this point.
        //
        // If the action might return any object, then assume we don't know anything about it.
        if (declaredReturnType == typeof(object))
        {
            return null;
        }

        return declaredReturnType;
    }

    private static bool IsClientError(int statusCode)
    {
        return statusCode >= 400 && statusCode < 500;
    }

    private static bool HasSignificantMetadataProvider(IReadOnlyList<IApiResponseMetadataProvider> providers)
    {
        for (var i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];

            if (provider is ProducesAttribute producesAttribute && producesAttribute.Type is null)
            {
                // ProducesAttribute that does not specify type is considered not significant.
                continue;
            }

            // Any other IApiResponseMetadataProvider is considered significant
            return true;
        }

        return false;
    }
}
