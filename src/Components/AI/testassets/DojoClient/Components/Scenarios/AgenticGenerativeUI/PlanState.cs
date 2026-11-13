// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient.Components.Scenarios.AgenticGenerativeUI;

public sealed class PlanState
{
    public List<PlanStep> Steps { get; set; } = [];
}

public sealed class PlanStep
{
    public string Description { get; set; } = "";

    public string Status { get; set; } = "pending";
}
