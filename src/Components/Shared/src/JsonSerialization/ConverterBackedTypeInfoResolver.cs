// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

// A JS runtime describes several of the types it exchanges entirely through converters it registers
// on its own options at construction time — object and stream references, byte arrays, element
// references. Those types carry no metadata that could be generated ahead of time, and the converter
// instances are bound to a particular runtime, so they cannot be declared statically either.
// Resolving them from the options' own converters covers every one of them at once.
internal sealed class ConverterBackedTypeInfoResolver : IJsonTypeInfoResolver
{
    public static readonly ConverterBackedTypeInfoResolver Instance = new();

    private ConverterBackedTypeInfoResolver()
    {
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "A converter for the type is confirmed to exist first, so the created contract wraps that converter and never reflects over the type's members.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "A converter for the type is confirmed to exist first, so the created contract wraps that converter and never builds a collection or object contract dynamically.")]
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        // Only converters the runtime registered itself are considered; the built-in converters for
        // primitives and collections are not part of this collection, so this never takes over a
        // contract that a generated resolver later in the chain is meant to provide.
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
