using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Testing;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.Server.Test.Autobahn
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