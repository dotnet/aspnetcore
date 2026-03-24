// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

class E2EPublishedApp
{
    [JsonPropertyName("executable")]
    public string Executable { get; set; } = "";

    [JsonPropertyName("args")]
    public string Args { get; set; } = "";

    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectory { get; set; } = "";
}
