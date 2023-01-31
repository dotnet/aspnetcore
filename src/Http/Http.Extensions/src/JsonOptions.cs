// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

#nullable enable

namespace Microsoft.AspNetCore.Http.Json;

/// <summary>
/// Options to configure JSON serialization settings for <see cref="HttpRequestJsonExtensions"/>
/// and <see cref="HttpResponseJsonExtensions"/>.
/// </summary>
public class JsonOptions
{
    private static JsonSerializerOptions? _reflectionSerializerOptions;
    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        // Web defaults don't use the relex JSON escaping encoder.
        //
        // Because these options are for producing content that is written directly to the request
        // (and not embedded in an HTML page for example), we can use UnsafeRelaxedJsonEscaping.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal static JsonSerializerOptions ReflectionBasedSerializerOptions
    {
        [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use the 'DefaultSerializerOptions' property instead.")]
        [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the 'DefaultSerializerOptions' property instead.")]
        get => _reflectionSerializerOptions ??= new JsonSerializerOptions(DefaultSerializerOptions)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    // Use a copy so the defaults are not modified.
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; internal set; } = new JsonSerializerOptions(DefaultSerializerOptions);
}
