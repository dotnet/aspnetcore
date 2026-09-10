// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal interface IAdapter
{
    bool TryTraverse(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out object nextTarget,
        out string errorMessage);

    bool TryAdd(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage);

    bool TryRemove(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out string errorMessage);

    bool TryGet(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        out object value,
        out string errorMessage);

    bool TryReplace(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage);

    bool TryTest(
        object target,
        string segment,
        JsonSerializerOptions serializerOptions,
        object value,
        out string errorMessage);
}
