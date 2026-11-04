// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public interface IAdapter
{
    bool TryTraverse(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object nextTarget,
        out string errorMessage);

    bool TryAdd(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage);

    bool TryRemove(
        object target,
        string segment,
        IContractResolver contractResolver,
        out string errorMessage);

    bool TryGet(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object value,
        out string errorMessage);

    bool TryReplace(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage);

    bool TryTest(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage);
}
