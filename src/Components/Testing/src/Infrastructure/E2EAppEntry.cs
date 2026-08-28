// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

class E2EAppEntry
{
    internal const string StartupHookHarnessMode = "startupHook";
    internal const string CompiledHarnessMode = "compiled";

    [JsonPropertyName("executable")]
    public string Executable { get; set; } = "";

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "";

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("publicUrl")]
    public string? PublicUrl { get; set; }

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    [JsonPropertyName("harnessMode")]
    public string HarnessMode { get; set; } = StartupHookHarnessMode;
}
