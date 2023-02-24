// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

internal static class JsonSerializerOptionsExtensions
{
    private static DefaultJsonTypeInfoResolver? _defaultJsonTypeInfoResolver;

    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or ensure Microsoft.AspNetCore.EnsureJsonTrimmability=true for native AOT applications.")]
    public static void InitializeForReflection(this JsonSerializerOptions options)
    {
        options.TypeInfoResolver ??= _defaultJsonTypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
    }

    public static void EnsureConfigured(this JsonSerializerOptions options, bool markAsReadOnly = false)
    {
        if (!options.IsReadOnly)
        {
            // This might be removed once https://github.com/dotnet/runtime/issues/80529 is fixed
            if (!TrimmingAppContextSwitches.EnsureJsonTrimmability)
            {
#pragma warning disable IL2026 // Suppressed in Microsoft.AspNetCore.Http.Extensions.WarningSuppressions.xml
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                options.InitializeForReflection();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Suppressed in Microsoft.AspNetCore.Http.Extensions.WarningSuppressions.xml
            }

            if (markAsReadOnly)
            {
                options.MakeReadOnly();
            }
        }
    }
}
