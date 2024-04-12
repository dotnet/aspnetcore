// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal static class DefaultJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? CreateDefaultTypeInfoResolver() : JsonTypeInfoResolver.Combine(),
    };

    static DefaultJsonSerializerOptions()
    {
        Instance.MakeReadOnly();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This method only gets called when reflection is enabled for JsonSerializer")]
    static DefaultJsonTypeInfoResolver CreateDefaultTypeInfoResolver()
        => new();
}
