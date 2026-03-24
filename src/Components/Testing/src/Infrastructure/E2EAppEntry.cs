// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

class E2EAppEntry
{
    [JsonPropertyName("projectPath")]
    public string? ProjectPath { get; set; }

    [JsonPropertyName("publicUrl")]
    public string? PublicUrl { get; set; }

    [JsonPropertyName("published")]
    public E2EPublishedApp? Published { get; set; }

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}
