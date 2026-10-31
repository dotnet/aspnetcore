// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Http;

internal static class JsonSerializerExtensions
{
    public static bool HasKnownPolymorphism(this JsonTypeInfo jsonTypeInfo)
     => jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null;

    public static bool ShouldUseWith(this JsonTypeInfo jsonTypeInfo, [NotNullWhen(false)] Type? runtimeType)
     => runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.HasKnownPolymorphism();

    public static JsonTypeInfo GetReadOnlyTypeInfo(this JsonSerializerOptions options, Type type)
    {
        options.MakeReadOnly();
        return options.GetTypeInfo(type);
    }

    public static JsonTypeInfo GetRequiredTypeInfo(this JsonSerializerContext context, Type type)
        => context.GetTypeInfo(type) ?? throw new InvalidOperationException($"Unable to obtain the JsonTypeInfo for type '{type.FullName}' from the context '{context.GetType().FullName}'.");
}
