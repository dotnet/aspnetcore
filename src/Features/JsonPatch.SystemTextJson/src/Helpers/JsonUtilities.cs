// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;

internal static class JsonUtilities
{
    public static bool DeepEquals(object a, object b, JsonSerializerOptions serializerOptions)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if (a is JsonNode nodeA && b is JsonNode nodeB)
        {
            return JsonNode.DeepEquals(nodeA, nodeB);
        }

        using var docA = TryGetJsonElement(a, serializerOptions, out var elementA);
        using var docB = TryGetJsonElement(b, serializerOptions, out var elementB);

        return JsonElement.DeepEquals(elementA, elementB);
    }

    private static IDisposable TryGetJsonElement(object item, JsonSerializerOptions serializerOptions, out JsonElement element)
    {
        IDisposable result = null;
        if (item is JsonElement jsonElement)
        {
            element = jsonElement;
        }
        else
        {
            var docA = JsonSerializer.SerializeToDocument(item, serializerOptions);
            element = docA.RootElement;
            result = docA;
        }

        return result;
    }
}
