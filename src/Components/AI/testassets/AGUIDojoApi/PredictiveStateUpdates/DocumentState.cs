// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.PredictiveStateUpdates;

internal sealed class DocumentState
{
    [JsonPropertyName("document")]
    public string Document { get; set; } = "";
}
