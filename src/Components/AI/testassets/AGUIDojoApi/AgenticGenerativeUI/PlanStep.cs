// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.AgenticGenerativeUI;

internal sealed class PlanStep
{
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("status")]
    public PlanStepStatus Status { get; set; } = PlanStepStatus.Pending;
}
