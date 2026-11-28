// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace PlatformTestApp.Client.Components;

public sealed class MessagingPayload
{
    [JsonPropertyName("__jsObjectId")]
    public required string Text { get; init; }

    public int Version { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MessagingPayload))]
[JsonSerializable(typeof(List<MessagingPayload>))]
[JsonSerializable(typeof(int?))]
internal partial class MessagingJsonContext : JsonSerializerContext
{
}
