// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal static class PersistentStateSerializationOptions
{
    public static JsonSerializerOptions Create(IJsonTypeInfoResolver? applicationResolver)
    {
        var options = new JsonSerializerOptions(JsonSerializerOptionsProvider.Options);
        options.TypeInfoResolverChain.Clear();
        options.TypeInfoResolverChain.Add(PersistentStateJsonContext.Default);

        if (applicationResolver is not null)
        {
            options.TypeInfoResolverChain.Add(applicationResolver);
        }

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

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
internal sealed partial class PersistentStateJsonContext : JsonSerializerContext;
