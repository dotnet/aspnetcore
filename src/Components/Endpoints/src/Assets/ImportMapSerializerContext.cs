// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

[JsonSerializable(typeof(ImportMapDefinition))]
internal partial class ImportMapSerializerContext : JsonSerializerContext
{
    private static ImportMapSerializerContext? _customEncoder;

    public static ImportMapSerializerContext CustomEncoder => _customEncoder ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    });
}
