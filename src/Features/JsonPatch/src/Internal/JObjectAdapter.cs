// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

public class JObjectAdapter : IAdapter
{
    public virtual bool TryAdd(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        var obj = (JObject)target;

        obj[segment] = value != null ? JToken.FromObject(value) : JValue.CreateNull();

        errorMessage = null;
        return true;
    }

    public virtual bool TryGet(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object value,
        out string errorMessage)
    {
        var obj = (JObject)target;

        if (!obj.TryGetValue(segment, out var valueAsToken))
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
        IContractResolver contractResolver,
        out string errorMessage)
    {
        var obj = (JObject)target;

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
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        var obj = (JObject)target;

        if (!obj.ContainsKey(segment))
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        obj[segment] = value != null ? JToken.FromObject(value) : JValue.CreateNull();

        errorMessage = null;
        return true;
    }

    public virtual bool TryTest(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        var obj = (JObject)target;

        if (!obj.TryGetValue(segment, out var currentValue))
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
        {
            errorMessage = Resources.FormatValueForTargetSegmentCannotBeNullOrEmpty(segment);
            return false;
        }

        if (!JToken.DeepEquals(JsonConvert.SerializeObject(currentValue), JsonConvert.SerializeObject(value)))
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
        IContractResolver contractResolver,
        out object nextTarget,
        out string errorMessage)
    {
        var obj = (JObject)target;

        if (!obj.TryGetValue(segment, out var nextTargetToken))
        {
            nextTarget = null;
            errorMessage = null;
            return false;
        }

        nextTarget = nextTargetToken;
        errorMessage = null;
        return true;
    }
}
