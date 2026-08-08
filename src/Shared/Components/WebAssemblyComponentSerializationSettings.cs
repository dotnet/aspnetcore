// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
#if COMPONENTS
using Microsoft.AspNetCore.Components.Infrastructure;
#endif

namespace Microsoft.AspNetCore.Components;

internal static class WebAssemblyComponentSerializationSettings
{
    public static readonly JsonSerializerOptions JsonSerializationOptions = CreateOptions();

    // See ServerComponentSerializationSettings: the framework's marker DTOs resolve from generated
    // contracts so that the reflection resolver is needed only for application parameter values.
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.TypeInfoResolverChain.Add(WebAssemblyComponentJsonContext.Default);
#if COMPONENTS
        options.TypeInfoResolverChain.Add(ComponentMarkerJsonTypeInfoResolver.Instance);
#elif COMPONENTS_WEBASSEMBLY
        options.TypeInfoResolverChain.Add(GetComponentMarkerJsonTypeInfoResolver(null));
#endif

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }

        return options;
    }

#if COMPONENTS_WEBASSEMBLY
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetResolver")]
    private static extern IJsonTypeInfoResolver GetComponentMarkerJsonTypeInfoResolver(
        [UnsafeAccessorType(
            "Microsoft.AspNetCore.Components.Infrastructure.ComponentMarkerJsonTypeInfoResolver, Microsoft.AspNetCore.Components")]
        object? target);
#endif

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

/// <summary>
/// Contracts for the DTOs the framework itself writes into a WebAssembly component marker.
/// </summary>
/// <remarks>
/// Only framework types are covered, so parameter values still resolve through the reflection
/// resolver or the application's own generated resolver.
/// </remarks>
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