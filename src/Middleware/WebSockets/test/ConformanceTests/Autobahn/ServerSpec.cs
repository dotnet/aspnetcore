// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
    public class ServerSpec
    {
        public string Name { get; }
        public string Url { get; }

        public ServerSpec(string name, string url)
        {
            Name = name;
            Url = url;
        }

        public JObject GetJson() => new JObject(
            new JProperty("agent", Name),
            new JProperty("url", Url),
            new JProperty("options", new JObject(
                new JProperty("version", 18))));
    }
}