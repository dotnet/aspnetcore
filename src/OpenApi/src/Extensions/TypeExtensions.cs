// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.OpenApi;

internal static class TypeExtensions
{
    private const string JsonPatchDocumentNamespace = "Microsoft.AspNetCore.JsonPatch.SystemTextJson";
    private const string JsonPatchDocumentName = "JsonPatchDocument";
    private const string JsonPatchDocumentNameOfT = "JsonPatchDocument`1";

    public static bool IsJsonPatchDocument(this Type type)
    {
        // We cannot depend on the actual runtime type as
        // Microsoft.AspNetCore.JsonPatch.SystemTextJson is not
        // AoT compatible so cannot be referenced by Microsoft.AspNetCore.OpenApi.
        var modelType = type;

        while (modelType != null && modelType != typeof(object))
        {
            if (modelType.Namespace == JsonPatchDocumentNamespace &&
                (modelType.Name == JsonPatchDocumentName ||
                 (modelType.IsGenericType && modelType.GenericTypeArguments.Length == 1 && modelType.Name == JsonPatchDocumentNameOfT)))
            {
                return true;
            }

            modelType = modelType.BaseType;
        }

        return false;
    }

    public static bool ShouldApplyNullableResponseSchema(this ApiResponseType apiResponseType, ApiDescription apiDescription)
    {
        // Get the MethodInfo from the ActionDescriptor
        var responseType = apiResponseType.Type;
        var methodInfo = apiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
            ? controllerActionDescriptor.MethodInfo
            : apiDescription.ActionDescriptor.EndpointMetadata.OfType<MethodInfo>().SingleOrDefault();

        if (methodInfo is null)
        {
            return false;
        }

        var returnType = methodInfo.ReturnType;
        if (returnType.IsGenericType &&
            (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returnType = returnType.GetGenericArguments()[0];
        }
        if (returnType != responseType)
        {
            return false;
        }

        if (returnType.IsValueType)
        {
            return apiResponseType.ModelMetadata?.IsNullableValueType ?? false;
        }

        var nullabilityInfoContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityInfoContext.Create(methodInfo.ReturnParameter);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }

    public static bool ShouldApplyNullableRequestSchema(this ApiParameterDescription apiParameterDescription)
    {
        var parameterType = apiParameterDescription.Type;
        if (parameterType is null)
        {
            return false;
        }

        if (apiParameterDescription.ParameterDescriptor is not IParameterInfoParameterDescriptor { ParameterInfo: { } parameterInfo })
        {
            return false;
        }

        if (parameterType.IsValueType)
        {
            return apiParameterDescription.ModelMetadata?.IsNullableValueType ?? false;
        }

        var nullabilityInfoContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityInfoContext.Create(parameterInfo);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }

    public static bool ShouldApplyNullablePropertySchema(this JsonPropertyInfo jsonPropertyInfo)
    {
        if (jsonPropertyInfo.AttributeProvider is not PropertyInfo propertyInfo)
        {
            return false;
        }

        var nullabilityInfoContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityInfoContext.Create(propertyInfo);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }
}
