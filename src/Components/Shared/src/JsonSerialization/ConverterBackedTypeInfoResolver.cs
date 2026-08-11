// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal sealed class ConverterBackedTypeInfoResolver : IJsonTypeInfoResolver
{
    public static readonly ConverterBackedTypeInfoResolver Instance = new();

    private ConverterBackedTypeInfoResolver()
    {
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "A registered converter handles the type, so the contract never reflects over its members.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "A registered converter handles the type, so no collection or object contract is generated dynamically.")]
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        foreach (var converter in options.Converters)
        {
            if (converter.CanConvert(type))
            {
                return JsonTypeInfo.CreateJsonTypeInfo(type, options);
            }
        }

        return null;
    }
}
