// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Tasks;

[DataContract]
internal class E2EManifestModel
{
    [DataMember(Name = "apps")]
    public Dictionary<string, E2EAppEntryModel> Apps { get; set; } = new();
}

[DataContract]
internal class E2EAppEntryModel
{
    [DataMember(Name = "executable")]
    public string Executable { get; set; } = "";

    [DataMember(Name = "arguments")]
    public string Arguments { get; set; } = "";

    [DataMember(Name = "workingDirectory")]
    public string WorkingDirectory { get; set; } = "";

    [DataMember(Name = "publicUrl")]
    public string PublicUrl { get; set; } = "";

    [DataMember(Name = "environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}
