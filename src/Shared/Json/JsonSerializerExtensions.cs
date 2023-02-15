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

    public static bool IsValid(this JsonTypeInfo jsonTypeInfo, [NotNullWhen(false)] Type? runtimeType)
     => runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.HasKnownPolymorphism();

    public static JsonTypeInfo GetRequiredTypeInfo(this JsonSerializerContext context, Type type)
        => context.GetTypeInfo(type) ?? throw new InvalidOperationException($"Unable to obtain the JsonTypeInfo for type '{type.FullName}' from the context '{context.GetType().FullName}'.");

    public static Task WriteToResponseAsync<T>(this JsonTypeInfo<T> jsonTypeInfo, HttpResponse response, T? value, JsonSerializerOptions options, string? contentType = null)
    {
        var runtimeType = value?.GetType();

        if (jsonTypeInfo.IsValid(runtimeType))
        {
            // In this case the polymorphism is not
            // relevant for us and will be handled by STJ, if needed.
            return HttpResponseJsonExtensions.WriteAsJsonAsync(response, value!, jsonTypeInfo, contentType: contentType, default);
        }

        // Since we don't know the type's polymorphic characteristics
        // our best option is use the runtime type, so,
        // call WriteAsJsonAsync() with the runtime type to serialize the runtime type rather than the declared type
        // and avoid source generators issues.
        // https://github.com/dotnet/aspnetcore/issues/43894
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism
        var runtimeTypeInfo = options.GetTypeInfo(runtimeType);
        return HttpResponseJsonExtensions.WriteAsJsonAsync(response, value!, runtimeTypeInfo, contentType: contentType, default);
    }
}
