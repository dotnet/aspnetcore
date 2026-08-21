// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient.Components.Scenarios.ToolBasedGenerativeUI;

public sealed class HaikuData
{
    public List<string> Japanese { get; set; } = [];

    public List<string> English { get; set; } = [];

    public string ImageName { get; set; } = "";

    public string Gradient { get; set; } = "";
}
