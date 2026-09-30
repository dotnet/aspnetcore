// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.AgenticGenerativeUI;

internal sealed class Plan
{
    [JsonPropertyName("steps")]
    public List<PlanStep> Steps { get; set; } = [];
}
