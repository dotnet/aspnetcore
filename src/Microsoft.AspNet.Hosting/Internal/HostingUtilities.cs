// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Hosting.Internal
{
    public static class HostingUtilities
    {
        public static string GetWebRoot(string applicationBasePath)
        {
            var webroot = applicationBasePath;
            using (var stream = File.OpenRead(Path.Combine(applicationBasePath, "project.json")))
            {
                using (var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    var project = JObject.Load(reader);
                    JToken token;
                    if (project.TryGetValue("webroot", out token))
                    {
                        webroot = Path.Combine(applicationBasePath, token.ToString());
                    }
                }
            }
            return Path.GetFullPath(webroot);
        }
    }
}
