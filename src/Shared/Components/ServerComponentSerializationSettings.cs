// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal static class ServerComponentSerializationSettings
{
    public const string DataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1";

    public static readonly JsonSerializerOptions JsonSerializationOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.TypeInfoResolverChain.Add(ServerComponentJsonContext.Default);
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
