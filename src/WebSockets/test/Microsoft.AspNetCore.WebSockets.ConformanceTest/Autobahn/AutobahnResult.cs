using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
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
}