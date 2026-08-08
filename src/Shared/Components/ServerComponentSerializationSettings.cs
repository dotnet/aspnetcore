// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
#if COMPONENTS || COMPONENTS_SERVER
using Microsoft.AspNetCore.Components.Infrastructure;
#endif

namespace Microsoft.AspNetCore.Components;

internal static class ServerComponentSerializationSettings
{
    public const string DataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1";

    public static readonly JsonSerializerOptions JsonSerializationOptions = CreateOptions();

    // The framework's own marker DTOs resolve from generated contracts, so a circuit can be bootstrapped
    // with reflection-based serialization disabled. Reflection stays in the chain, last, whenever it is
    // enabled at all, so that an application's parameter values keep round-tripping exactly as before.
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.TypeInfoResolverChain.Add(ServerComponentJsonContext.Default);
#if COMPONENTS || COMPONENTS_SERVER
        options.TypeInfoResolverChain.Add(ComponentMarkerJsonTypeInfoResolver.Instance);
#endif

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

    // This setting is not configurable, but realistically we don't expect an app to take more than 30 seconds from when
    // it got rendered to when the circuit got started, and having an expiration on the serialized server-components helps
    // prevent old payloads from being replayed.
    public static readonly TimeSpan DataExpiration = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Contracts for the DTOs the framework itself writes into a server component marker.
/// </summary>
/// <remarks>
/// These types are fixed by the framework, so their contracts are generated rather than reflected over.
/// Parameter <em>values</em> are deliberately not covered: they are application types, and their
/// contracts come from the reflection resolver or from the application's own generated resolver.
/// </remarks>
[JsonSerializable(typeof(ServerComponent))]
[JsonSerializable(typeof(ComponentMarker))]
[JsonSerializable(typeof(IEnumerable<ComponentMarker>))]
[JsonSerializable(typeof(ComponentEndMarker))]
[JsonSerializable(typeof(ComponentMarkerKey))]
[JsonSerializable(typeof(ComponentParameter))]
[JsonSerializable(typeof(ComponentParameter[]))]
[JsonSerializable(typeof(IList<ComponentParameter>))]
[JsonSerializable(typeof(IList<object>))]
[JsonSerializable(typeof(object[]))]
#if COMPONENTS || COMPONENTS_SERVER
[JsonSerializable(typeof(SerializedRenderFragment))]
[JsonSerializable(typeof(RenderTreeNode))]
[JsonSerializable(typeof(List<RenderTreeNode>))]
[JsonSerializable(typeof(RenderTreeAttribute))]
#endif
internal sealed partial class ServerComponentJsonContext : JsonSerializerContext;
