// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class AutobahnSpec
{
    public string OutputDirectory { get; }
    public IList<ServerSpec> Servers { get; } = new List<ServerSpec>();
    public IList<string> Cases { get; } = new List<string>();
    public IList<string> ExcludedCases { get; } = new List<string>();

    public AutobahnSpec(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
    }

    public AutobahnSpec WithServer(string name, string url)
    {
        Servers.Add(new ServerSpec(name, url));
        return this;
    }

    public AutobahnSpec IncludeCase(params string[] caseSpecs)
    {
        foreach (var caseSpec in caseSpecs)
        {
            Cases.Add(caseSpec);
        }
        return this;
    }

    public AutobahnSpec ExcludeCase(params string[] caseSpecs)
    {
        foreach (var caseSpec in caseSpecs)
        {
            ExcludedCases.Add(caseSpec);
        }
        return this;
    }

    public void WriteJson(string file)
    {
        File.WriteAllText(file, GetJson().ToString(Formatting.Indented));
    }

    public JObject GetJson() => new JObject(
        new JProperty("options", new JObject(
            new JProperty("failByDrop", false))),
        new JProperty("outdir", OutputDirectory),
        new JProperty("servers", new JArray(Servers.Select(s => s.GetJson()).ToArray())),
        new JProperty("cases", new JArray(Cases.ToArray())),
        new JProperty("exclude-cases", new JArray(ExcludedCases.ToArray())),
        new JProperty("exclude-agent-cases", new JObject()));
}
