// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IntegrationTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class AutobahnServerResult
{
    public ServerType Server { get; }
    public bool Ssl { get; }
    public string Environment { get; }
    public string Name { get; }
    public IEnumerable<AutobahnCaseResult> Cases { get; }

    public AutobahnServerResult(string name, IEnumerable<AutobahnCaseResult> cases)
    {
        Name = name;

        var splat = name.Split('|');
        if (splat.Length < 3)
        {
            throw new FormatException("Results incorrectly formatted");
        }

        Server = (ServerType)Enum.Parse(typeof(ServerType), splat[0]);
        Ssl = string.Equals(splat[1], "SSL", StringComparison.Ordinal);
        Environment = splat[2];
        Cases = cases;
    }

    public static AutobahnServerResult FromJson(JProperty prop)
    {
        var valueObj = ((JObject)prop.Value);
        return new AutobahnServerResult(prop.Name, valueObj.Properties().Select(AutobahnCaseResult.FromJson));
    }
}
