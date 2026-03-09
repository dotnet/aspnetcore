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
    // ApiResponseType has Type, StatusCode and ApiResponseFormats (which keeps MediaTypes aka Content-Type)
    // so we need to distinguish per StatusCode+Type here
    internal readonly record struct ResponseKey(int StatusCode, Type? DeclaredType);

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

            // scope here is the highest - those are "significant" metadata providers, so we use the highest scope
            var apiConventionedAttributes = apiConventionResult.ResponseMetadataProviders.Select(x => new ApiResponseMetadataProviderWithScope(x, scope: int.MaxValue));
            responseMetadataAttributes.AddRange(apiConventionedAttributes);
        }

        var defaultErrorType = typeof(void);
        if (action.Properties.TryGetValue(typeof(ProducesErrorResponseTypeAttribute), out result))
        {
            defaultErrorType = ((ProducesErrorResponseTypeAttribute)result!).Type;
        }

        var producesResponseMetadata = action.EndpointMetadata
            .OfType<IProducesResponseTypeMetadata>()
            .Where(m => m is not IApiResponseMetadataProvider)
            .ToList();
        var apiResponseTypes = GetApiResponseTypes(responseMetadataAttributes, producesResponseMetadata, runtimeReturnType, defaultErrorType);
        return apiResponseTypes;
    }

    private static List<ApiResponseMetadataProviderWithScope> GetResponseMetadataAttributes(ControllerActionDescriptor action)
    {
        if (action.FilterDescriptors == null)
        {
            return new List<ApiResponseMetadataProviderWithScope>();
        }

        // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
        // while searching for a filter that implements IApiResponseMetadataProvider.
        //
        // The workaround for that is to implement the metadata interface on the IFilterFactory.
        return action.FilterDescriptors
            .Where(fd => fd.Filter is IApiResponseMetadataProvider)
            .Select(fd => new ApiResponseMetadataProviderWithScope((IApiResponseMetadataProvider)fd.Filter, fd.Scope))
            .ToList();
    }

    private ICollection<ApiResponseType> GetApiResponseTypes(
       IReadOnlyList<ApiResponseMetadataProviderWithScope> responseMetadataAttributes,
       IReadOnlyList<IProducesResponseTypeMetadata> producesResponseMetadata,
       Type? type,
       Type defaultErrorType)
    {
        var contentTypes = new MediaTypeCollection();
        var responseTypeMetadataProviders = _mvcOptions.OutputFormatters.OfType<IApiResponseTypeMetadataProvider>();

        // Read response types from endpoint metadata (IProducesResponseTypeMetadata),
        // e.g. from TypedResults or .Produces<T>() extension methods.
        var endpointResponseTypes = ReadEndpointResponseMetadata(
            producesResponseMetadata,
            type,
            responseTypeMetadataProviders,
            _modelMetadataProvider);

        // Read response types from filter attributes (IApiResponseMetadataProvider),
        // e.g. [ProducesResponseType], [Produces], and conventions.
        var attributeResponseTypes = ReadAttributeResponseMetadata(
            responseMetadataAttributes,
            type,
            defaultErrorType,
            contentTypes,
            out var _,
            responseTypeMetadataProviders);

        // Attribute metadata takes precedence: for any status code defined by attributes,
        // all endpoint entries for that status code are replaced by the attribute entries.
        var attributeStatusCodes = attributeResponseTypes.Values.Select(r => r.StatusCode).ToHashSet();
        var responseTypes = endpointResponseTypes
            .Where(kvp => !attributeStatusCodes.Contains(kvp.Key.StatusCode))
            .Concat(attributeResponseTypes)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Set the default status only when no status has already been set explicitly
        if (responseTypes.Count == 0 && type != null)
        {
            var defaultKey = new ResponseKey(StatusCodes.Status200OK, type);
            responseTypes.Add(defaultKey, new ApiResponseType
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

        return responseTypes.Values
            .OrderBy(responseType => responseType.StatusCode)
            .ThenBy(responseType => responseType.Type?.Name)
            .ThenBy(responseType => responseType.ApiResponseFormats.FirstOrDefault()?.MediaType)
            .ToList();
    }

    // Shared with EndpointMetadataApiDescriptionProvider for Minimal API
    internal static Dictionary<ResponseKey, ApiResponseType> ReadAttributeResponseMetadata(
        IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
        Type? type,
        Type? defaultErrorType,
        MediaTypeCollection contentTypes,
        out bool errorSetByDefault,
        IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
        IModelMetadataProvider? modelMetadataProvider = null)
    {
        // Minimal API does not have scopes — all metadata lives at the same level.
        // Using the same scope (0) for all entries ensures that entries with the same
        // status code and type are merged (e.g., different content types) rather than
        // one overriding the other.
        var responseMetadataAttributesWithScope = responseMetadataAttributes
            .Select(provider => new ApiResponseMetadataProviderWithScope(provider, scope: 0))
            .ToList();

        return ReadAttributeResponseMetadata(responseMetadataAttributesWithScope, type, defaultErrorType, contentTypes, out errorSetByDefault, responseTypeMetadataProviders, modelMetadataProvider);
    }

    internal static Dictionary<ResponseKey, ApiResponseType> ReadAttributeResponseMetadata(
        IReadOnlyList<ApiResponseMetadataProviderWithScope> responseMetadataAttributes,
        Type? type,
        Type? defaultErrorType,
        MediaTypeCollection contentTypes,
        out bool errorSetByDefault,
        IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
        IModelMetadataProvider? modelMetadataProvider = null)
    {
        errorSetByDefault = false;
        var results = new Dictionary<ResponseKey, ApiResponseType>();
        var statusCodeScopes = new Dictionary<int, int>();
        var contentTypesAlreadySet = false;

        // Get the content type that the action explicitly set to support.
        // Walk through all 'filter' attributes in descending scope order. Descending order ensures
        // that higher-scope entries (e.g., action-level) are processed first, so lower-scope entries
        // for the same status code can be skipped.
        if (responseMetadataAttributes != null)
        {
            foreach (var metadataAttributeWithScope in responseMetadataAttributes.OrderByDescending(attr => attr.Scope))
            {
                var metadataAttribute = metadataAttributeWithScope.Provider;
                var attributeScope = metadataAttributeWithScope.Scope;

                // All IApiResponseMetadataProvider attributes, except for ProducesResponseTypeAttribute
                // (which gets its own content type collection) and ProducesDefaultResponseTypeAttribute
                // (whose SetContentTypes is a no-op), can set shared content types. Since we iterate
                // in descending scope order, only the first (highest-scope) such attribute should set
                // content types. Lower-scope attributes must not overwrite content types already set
                // by a higher-scope one (e.g., action-level Produces overrides controller-level Produces).
                if (metadataAttribute is not ProducesResponseTypeAttribute
                    and not ProducesDefaultResponseTypeAttribute
                    && !contentTypesAlreadySet)
                {
                    metadataAttribute.SetContentTypes(contentTypes);
                    contentTypesAlreadySet = true;
                }

                var statusCode = metadataAttribute.StatusCode;

                var apiResponseType = new ApiResponseType
                {
                    Type = metadataAttribute.Type,
                    StatusCode = statusCode,
                    IsDefaultResponse = metadataAttribute is IApiDefaultResponseMetadataProvider,
                    Description = metadataAttribute.Description
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

                // We special case the handling of ProducesResponseTypeAttributes since
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
                    var key = new ResponseKey(apiResponseType.StatusCode, apiResponseType.Type);

                    if (statusCodeScopes.TryGetValue(statusCode, out var existingScope))
                    {
                        if (attributeScope > existingScope)
                        {
                            statusCodeScopes[statusCode] = attributeScope;
                            results[key] = apiResponseType;
                        }
                        else if (attributeScope == existingScope)
                        {
                            // Same scope, same key: merge content types
                            if (results.TryGetValue(key, out var existingEntry))
                            {
                                MergeApiResponseFormats(existingEntry, apiResponseType);
                            }
                            else
                            {
                                // Same scope, different type: add alongside
                                results[key] = apiResponseType;
                            }
                        }
                        // attributeScope < existingScope: skip, higher scope already claimed this status code
                    }
                    else
                    {
                        statusCodeScopes[statusCode] = attributeScope;
                        results[key] = apiResponseType;
                    }
                }
            }
        }

        return results;
    }

    internal static Dictionary<ResponseKey, ApiResponseType> ReadEndpointResponseMetadata(
        IReadOnlyList<IProducesResponseTypeMetadata> responseMetadata,
        Type? inferredType,
        IEnumerable<IApiResponseTypeMetadataProvider>? responseTypeMetadataProviders = null,
        IModelMetadataProvider? modelMetadataProvider = null)
    {
        var results = new Dictionary<ResponseKey, ApiResponseType>();

        foreach (var metadata in responseMetadata)
        {
            // Skip IResult types that implement IEndpointMetadataProvider (built-in framework types like TypedResults)
            // since they handle their own metadata population. Custom IResult implementations that don't implement
            // IEndpointMetadataProvider should be included in response metadata for API documentation.
            if (typeof(IResult).IsAssignableFrom(metadata.Type) && typeof(IEndpointMetadataProvider).IsAssignableFrom(metadata.Type))
            {
                continue;
            }

            var statusCode = metadata.StatusCode;

            var apiResponseType = new ApiResponseType
            {
                Type = metadata.Type,
                StatusCode = statusCode,
            };

            if (apiResponseType.Type == null)
            {
                if (inferredType != null && (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created))
                {
                    // Allow setting the response type from the return type of the method if it has
                    // not been set explicitly by the method.
                    apiResponseType.Type = inferredType;
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
                // ── Controller example ──────────────────────────────────────────────────
                //
                //   For controllers, after our .Where(m => m is not IApiResponseMetadataProvider) filter,
                //   responseMetadata is typically empty (attributes are handled by ReadAttributeResponseMetadata).
                //   So this removal logic rarely fires for controllers.
                //
                // ── Minimal API example (POCO return, inferredType != void) ─────────────────────
                //
                //   app.MapGet("/", () => new Product())      // inferredType = typeof(Product)
                //       .Produces(200)                        // metadata: (200, null) → inferred as Product
                //       .Produces<Customer>(200, "text/xml"); // metadata: (200, Customer)
                //
                //   On 2nd iteration will remove (200, Product) because DeclaredType == inferredType (Product)
                //     results = { (200, Customer) }
                //
                // ── Minimal API example (IResult return, type == void) ──────────────────
                //
                //   app.MapPost("/", () => TypedResults.Ok(new Product()))  // type = void (IResult)
                //       .Produces<Customer>(200);
                //
                //   type = void → `type != typeof(void)` is false → removal is SKIPPED.
                //   Both TypedResults' (200, Product) and .Produces<Customer>(200) coexist.
                //   This is expected: we cannot distinguish framework-inferred metadata from
                //   user-explicit metadata when both are IProducesResponseTypeMetadata.
                //
                // ── Minimal API example (POCO return, same type merges) ─────────────────
                //
                //   app.MapGet("/", () => new Product())      // type = typeof(Product)
                //       .Produces<Product>(200, "app/json")   // metadata: (200, Product)
                //       .Produces<Product>(200, "app/xml");   // metadata: (200, Product)
                //
                //   For same statusCode+types will merge the ApiResponseType into results = { (200, Product) with json+xml }
                // 
                if (inferredType != null && inferredType != typeof(void) && apiResponseType.Type != inferredType)
                {
                    var inferredTypeKey = new ResponseKey(apiResponseType.StatusCode, inferredType);
                    if (results.TryGetValue(inferredTypeKey, out _))
                    {
                        results.Remove(inferredTypeKey);
                    }
                }

                var key = new ResponseKey(apiResponseType.StatusCode, apiResponseType.Type);
                if (results.TryGetValue(key, out var existingEntry))
                {
                    // Same key: merge formats
                    MergeApiResponseFormats(existingEntry, apiResponseType);
                }
                else
                {
                    results[key] = apiResponseType;
                }
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

    private static void MergeApiResponseFormats(ApiResponseType existing, ApiResponseType newEntry)
    {
        foreach (var format in newEntry.ApiResponseFormats)
        {
            if (!existing.ApiResponseFormats.Any(f => f.MediaType == format.MediaType))
            {
                existing.ApiResponseFormats.Add(format);
            }
        }

        // rewrite description
        if (newEntry.Description is not null)
        {
            existing.Description = newEntry.Description;
        }
    }

    private static bool IsClientError(int statusCode)
    {
        return statusCode >= 400 && statusCode < 500;
    }

    private static bool HasSignificantMetadataProvider(IReadOnlyList<ApiResponseMetadataProviderWithScope> providers)
    {
        for (var i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];

            if (provider.Provider is ProducesAttribute producesAttribute && producesAttribute.Type is null)
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
