// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

internal static class WebAssemblyComponentSerializationSettings
{
    public static readonly JsonSerializerOptions JsonSerializationOptions = CreateOptions();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Component parameter types are preserved for reflection-based serialization.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Blazor WebAssembly supports reflection-based serialization under WebAssembly AOT.")]
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }
}
