// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    internal class ApiResponseTypeProvider
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
            if (responseMetadataAttributes.Count == 0 &&
                action.Properties.TryGetValue(typeof(ApiConventionResult), out var result))
            {
                // Action does not have any conventions. Use conventions on it if present.
                var apiConventionResult = (ApiConventionResult)result;
                responseMetadataAttributes = apiConventionResult.ResponseMetadataProviders;
            }

            var apiResponseTypes = GetApiResponseTypes(responseMetadataAttributes, runtimeReturnType);
            return apiResponseTypes;
        }

        private IReadOnlyList<IApiResponseMetadataProvider> GetResponseMetadataAttributes(ControllerActionDescriptor action)
        {
            if (action.FilterDescriptors == null)
            {
                return Array.Empty<IApiResponseMetadataProvider>();
            }

            // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
            // while searching for a filter that implements IApiResponseMetadataProvider.
            //
            // The workaround for that is to implement the metadata interface on the IFilterFactory.
            return action.FilterDescriptors
                .Select(fd => fd.Filter)
                .OfType<IApiResponseMetadataProvider>()
                .ToArray();
        }

        private ICollection<ApiResponseType> GetApiResponseTypes(
           IReadOnlyList<IApiResponseMetadataProvider> responseMetadataAttributes,
           Type type)
        {
            var results = new Dictionary<int, ApiResponseType>();

            // Get the content type that the action explicitly set to support.
            // Walk through all 'filter' attributes in order, and allow each one to see or override
            // the results of the previous ones. This is similar to the execution path for content-negotiation.
            var contentTypes = new MediaTypeCollection();
            if (responseMetadataAttributes != null)
            {
                foreach (var metadataAttribute in responseMetadataAttributes)
                {
                    metadataAttribute.SetContentTypes(contentTypes);

                    ApiResponseType apiResponseType;

                    if (metadataAttribute is IApiDefaultResponseMetadataProvider)
                    {
                        apiResponseType = new ApiResponseType
                        {
                            IsDefaultResponse = true,
                            Type = metadataAttribute.Type,
                        };
                    }
                    else if (metadataAttribute.Type == typeof(void) &&
                        type != null &&
                        (metadataAttribute.StatusCode == StatusCodes.Status200OK || metadataAttribute.StatusCode == StatusCodes.Status201Created))
                    {
                        // ProducesResponseTypeAttribute's constructor defaults to setting "Type" to void when no value is specified.
                        // In this event, use the action's return type for 200 or 201 status codes. This lets you decorate an action with a
                        // [ProducesResponseType(201)] instead of [ProducesResponseType(201, typeof(Person)] when typeof(Person) can be inferred
                        // from the return type.
                        apiResponseType = new ApiResponseType
                        {
                            StatusCode = metadataAttribute.StatusCode,
                            Type = type,
                        };
                    }
                    else if (metadataAttribute.Type != null)
                    {
                        apiResponseType = new ApiResponseType
                        {
                            StatusCode = metadataAttribute.StatusCode,
                            Type = metadataAttribute.Type,
                        };
                    }
                    else
                    {
                        continue;
                    }

                    results[apiResponseType.StatusCode] = apiResponseType;
                }
            }


            // Set the default status only when no status has already been set explicitly
            if (results.Count == 0 && type != null)
            {
                results[StatusCodes.Status200OK] = new ApiResponseType
                {
                    StatusCode = StatusCodes.Status200OK,
                    Type = type,
                };
            }

            if (contentTypes.Count == 0)
            {
                contentTypes.Add((string)null);
            }

            var responseTypeMetadataProviders = _mvcOptions.OutputFormatters.OfType<IApiResponseTypeMetadataProvider>();

            foreach (var apiResponse in results.Values)
            {
                var responseType = apiResponse.Type;
                if (responseType == null || responseType == typeof(void))
                {
                    continue;
                }

                apiResponse.ModelMetadata = _modelMetadataProvider.GetMetadataForType(responseType);

                foreach (var contentType in contentTypes)
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
            }

            return results.Values;
        }

        private Type GetDeclaredReturnType(ControllerActionDescriptor action)
        {
            var declaredReturnType = action.MethodInfo.ReturnType;
            if (declaredReturnType == typeof(void) ||
                declaredReturnType == typeof(Task))
            {
                return typeof(void);
            }

            // Unwrap the type if it's a Task<T>. The Task (non-generic) case was already handled.
            Type unwrappedType = declaredReturnType;
            if (declaredReturnType.IsGenericType &&
                declaredReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                unwrappedType = declaredReturnType.GetGenericArguments()[0];
            }

            // If the method is declared to return IActionResult or a derived class, that information
            // isn't valuable to the formatter.
            if (typeof(IActionResult).IsAssignableFrom(unwrappedType))
            {
                return null;
            }

            // If we get here, the type should be a user-defined data type or an envelope type
            // like ActionResult<T>. The mapper service will unwrap envelopes.
            unwrappedType = _mapper.GetResultDataType(unwrappedType);
            return unwrappedType;
        }

        private Type GetRuntimeReturnType(Type declaredReturnType)
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
    }
}
