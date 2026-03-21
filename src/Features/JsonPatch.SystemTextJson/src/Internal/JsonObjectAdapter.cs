// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal class JsonObjectAdapter : IAdapter
{
    public virtual bool TryAdd(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage)
    {
        // Set the property specified by the `segment` argument to the given `value` of the `target` object.
        var obj = (JsonObject)target;

        obj[segment] = value != null ? JsonSerializer.SerializeToNode(value, serializerOptions) : GetJsonNull();

        errorMessage = null;
        return true;
    }

    public virtual bool TryGet(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out object value,
        out string errorMessage)
    {
        var obj = (JsonObject)target;

        if (!obj.TryGetPropertyValue(segment, out var valueAsToken))
        {
            value = null;
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        value = valueAsToken;
        errorMessage = null;
        return true;
    }

    public virtual bool TryRemove(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out string errorMessage)
    {
        var obj = (JsonObject)target;

        if (!obj.Remove(segment))
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryReplace(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage)
    {
        var obj = (JsonObject)target;

        int index = obj.IndexOf(segment);
        if (index == -1)
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        obj[index] = value != null ? JsonSerializer.SerializeToNode(value, serializerOptions) : GetJsonNull();

        errorMessage = null;
        return true;
    }

    public virtual bool TryTest(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage)
    {
        var obj = (JsonObject)target;

        if (!obj.TryGetPropertyValue(segment, out var currentValue))
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
        {
            errorMessage = Resources.FormatValueForTargetSegmentCannotBeNullOrEmpty(segment);
            return false;
        }

        if (!JsonUtilities.DeepEquals(currentValue, value, serializerOptions))
        {
            errorMessage = Resources.FormatValueNotEqualToTestValue(currentValue, value, segment);
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryTraverse(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out object nextTarget,
        out string errorMessage)
    {
        var obj = (JsonObject)target;

        if (!obj.TryGetPropertyValue(segment, out var nextTargetToken))
        {
            nextTarget = null;
            errorMessage = null;
            return false;
        }

        nextTarget = nextTargetToken;
        errorMessage = null;
        return true;
    }

    private static JsonValue GetJsonNull() => JsonValue.Create<object>(null);
}
