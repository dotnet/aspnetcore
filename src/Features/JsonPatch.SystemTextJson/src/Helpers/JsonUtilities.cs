// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;

internal static class JsonUtilities
{
    public static bool DeepEquals(object a, object b, JsonSerializerOptions serializerOptions)
    {
        return JsonObject.DeepEquals(
            JsonSerializer.SerializeToNode(a, serializerOptions),
            JsonSerializer.SerializeToNode(b, serializerOptions));
    }
}
