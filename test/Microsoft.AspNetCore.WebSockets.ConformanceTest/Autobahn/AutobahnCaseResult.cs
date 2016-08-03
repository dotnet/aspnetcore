using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
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
}