// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class AutobahnResult
{
    public IEnumerable<AutobahnServerResult> Servers { get; }

    public AutobahnResult(IEnumerable<AutobahnServerResult> servers)
    {
        Servers = servers;
    }

    public static AutobahnResult FromReportJson(JObject indexJson)
    {
        // Load the report
        return new AutobahnResult(indexJson.Properties().Select(AutobahnServerResult.FromJson));
    }
}
