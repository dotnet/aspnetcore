// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AIApp.Components.Scenarios.ToolBasedGenerativeUI;

public sealed class HaikuData
{
    public List<string> Japanese { get; set; } = new();
    public List<string> English { get; set; } = new();
    public string ImageName { get; set; } = "";
    public string Gradient { get; set; } = "";
}
