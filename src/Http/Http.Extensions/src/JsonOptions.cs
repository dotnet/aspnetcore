// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;

#nullable enable

namespace Microsoft.AspNetCore.Http.Json;

/// <summary>
/// Options to configure JSON serialization settings for <see cref="HttpRequestJsonExtensions"/>
/// and <see cref="HttpResponseJsonExtensions"/>.
/// </summary>
public class JsonOptions
{
    private static JsonSerializerOptions? _defaultSerializerOptionsInstance;
    private static readonly JsonSerializerOptions _defaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        // Web defaults don't use the relex JSON escaping encoder.
        //
        // Because these options are for producing content that is written directly to the request
        // (and not embedded in an HTML page for example), we can use UnsafeRelaxedJsonEscaping.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Use a copy so the defaults are not modified.
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(_defaultSerializerOptions);

    /// <summary>
    ///  Gets the default <see cref="JsonSerializerOptions"/> instance.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get => _defaultSerializerOptionsInstance ??= new JsonSerializerOptions(_defaultSerializerOptions).EnsureConfigured(markAsReadOnly: true); }

}
