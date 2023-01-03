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
    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        // Web defaults don't use the relex JSON escaping encoder.
        //
        // Because these options are for producing content that is written directly to the request
        // (and not embedded in an HTML page for example), we can use UnsafeRelaxedJsonEscaping.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

        // The JsonSerializerOptions.GetTypeInfo method is called directly and needs a defined resolver
        // setting the default resolver (reflection-based) but the user can overwrite it directly or calling
        // .AddContext<TContext>()
#pragma warning disable IL2026 // Suppressed in ILLink.Suppressions.LibraryBuild.xml 
        TypeInfoResolver = CreateDefaultTypeResolver()
#pragma warning restore IL2026 // Suppressed in ILLink.Suppressions.LibraryBuild.xml

    };

    // Use a copy so the defaults are not modified.
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(DefaultSerializerOptions);

    [RequiresUnreferencedCode("System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver might require types that cannot be statically analyzed and might need runtime code generation.")]
    [RequiresDynamicCode("System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver might require types that cannot be statically analyzed and might need runtime code generation. Enable EnsureJsonTrimmability feature switch for native AOT applications.")]
    private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
        => new DefaultJsonTypeInfoResolver();
}
