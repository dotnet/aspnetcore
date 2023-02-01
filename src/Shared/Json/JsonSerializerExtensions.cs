// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

internal static class JsonSerializerExtensions
{
    public static bool HasKnownPolymorphism(this JsonTypeInfo jsonTypeInfo)
     => jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null;

    public static bool IsValid(this JsonTypeInfo jsonTypeInfo, [NotNullWhen(false)] Type? runtimeType)
     => runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.HasKnownPolymorphism();

    public static JsonTypeInfo GetReadOnlyTypeInfo(this JsonSerializerOptions options, Type type)
    {
        options.Configure();

        return options.GetTypeInfo(type);
    }

    public static void Configure(this JsonSerializerOptions options)
    {
        if (!options.IsReadOnly)
        {
            if (!TrimmingAppContextSwitches.EnsureJsonTrimmability)
            {
#pragma warning disable IL2026 // Suppressed in Microsoft.AspNetCore.Http.Extensions.WarningSuppressions.xml
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                InitializeForReflection(options);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Suppressed in Microsoft.AspNetCore.Http.Extensions.WarningSuppressions.xml
            }

            options.MakeReadOnly();
        }
    }

    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Ensure Microsoft.AspNetCore.EnsureJsonTrimmability=true.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Ensure Microsoft.AspNetCore.EnsureJsonTrimmability=true.")]
    private static void InitializeForReflection(JsonSerializerOptions options)
    {
        var combinedResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, new DefaultJsonTypeInfoResolver());

        if (!options.IsReadOnly)
        {
            options.TypeInfoResolver = combinedResolver;
        }
    }
    public static JsonTypeInfo GetRequiredTypeInfo(this JsonSerializerContext context, Type type)
        => context.GetTypeInfo(type) ?? throw new InvalidOperationException($"Unable to obtain the JsonTypeInfo for type '{type.FullName}' from the context '{context.GetType().FullName}'.");
}
