// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class AutobahnCaseResult
{
    public string Name { get; }
    public string ActualBehavior { get; }

    public AutobahnCaseResult(string name, string actualBehavior)
    {
        Name = name;
        ActualBehavior = actualBehavior;
    }

    public static AutobahnCaseResult FromJson(JProperty prop)
    {
        var caseObj = (JObject)prop.Value;
        var actualBehavior = (string)caseObj["behavior"];
        return new AutobahnCaseResult(prop.Name, actualBehavior);
    }

    public bool BehaviorIs(params string[] behaviors)
    {
        return behaviors.Any(b => string.Equals(b, ActualBehavior, StringComparison.Ordinal));
    }
}
