// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
