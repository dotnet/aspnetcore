// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

namespace Microsoft.AspNetCore.OpenApi;

internal static class TypeExtensions
{
    public static bool IsJsonPatchDocument(this Type type)
    {
        if (type.IsAssignableTo(typeof(JsonPatchDocument)))
        {
            return true;
        }

        var modelType = type;

        while (modelType != null && modelType != typeof(object))
        {
            if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>))
            {
                return true;
            }

            modelType = modelType.BaseType;
        }

        return false;
    }
}
