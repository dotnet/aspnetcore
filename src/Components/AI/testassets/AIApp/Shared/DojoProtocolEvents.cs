// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIApp.Shared;

internal abstract class DojoProtocolEvent
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Timestamp { get; init; }

    [JsonPropertyName("rawEvent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? RawEvent { get; init; }
}

internal sealed class DojoStateSnapshotEvent : DojoProtocolEvent
{
    public override string Type => "STATE_SNAPSHOT";

    [JsonPropertyName("snapshot")]
    public required JsonElement Snapshot { get; init; }
}

internal sealed class DojoStateDeltaEvent : DojoProtocolEvent
{
    public override string Type => "STATE_DELTA";

    [JsonPropertyName("delta")]
    public required JsonElement Delta { get; init; }
}
