// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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