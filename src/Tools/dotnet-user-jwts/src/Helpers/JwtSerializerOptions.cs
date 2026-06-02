// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal static class JwtSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
    };
}

[JsonSourceGenerationOptions(AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, WriteIndented = true)]
[JsonSerializable(typeof(IDictionary<string, Jwt>))]
[JsonSerializable(typeof(Jwt))]
[JsonSerializable(typeof(Jwt[]))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(SigningKey))]
[JsonSerializable(typeof(SigningKey[]))]
internal sealed partial class JwtSerializerContext : JsonSerializerContext
{
}
