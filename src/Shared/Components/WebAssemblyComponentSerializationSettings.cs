// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal static class WebAssemblyComponentSerializationSettings
{
    public static readonly JsonSerializerOptions JsonSerializationOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.TypeInfoResolverChain.Add(WebAssemblyComponentJsonContext.Default);
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }

        return options;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();
}

[JsonSerializable(typeof(ComponentMarker))]
[JsonSerializable(typeof(ComponentEndMarker))]
[JsonSerializable(typeof(ComponentMarkerKey))]
[JsonSerializable(typeof(ComponentParameter))]
[JsonSerializable(typeof(ComponentParameter[]))]
[JsonSerializable(typeof(IList<ComponentParameter>))]
[JsonSerializable(typeof(IList<object>))]
[JsonSerializable(typeof(object[]))]
#if COMPONENTS || COMPONENTS_WEBASSEMBLY
[JsonSerializable(typeof(SerializedRenderFragment))]
[JsonSerializable(typeof(RenderTreeNode))]
[JsonSerializable(typeof(List<RenderTreeNode>))]
[JsonSerializable(typeof(RenderTreeAttribute))]
#endif
internal sealed partial class WebAssemblyComponentJsonContext : JsonSerializerContext;
