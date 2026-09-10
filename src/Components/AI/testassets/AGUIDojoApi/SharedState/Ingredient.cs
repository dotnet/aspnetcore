// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.SharedState;

internal sealed class Ingredient
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "";
}
