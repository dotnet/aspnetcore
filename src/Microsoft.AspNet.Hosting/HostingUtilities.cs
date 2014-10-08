// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingUtilities
    {
        internal static Tuple<string, string> SplitTypeName(string identifier)
        {
            string typeName = null;
            string assemblyName = identifier.Trim();
            var parts = identifier.Split(new[] { ',' }, 2);
            if (parts.Length == 2)
            {
                typeName = parts[0].Trim();
                assemblyName = parts[1].Trim();
            }
            return new Tuple<string, string>(typeName, assemblyName);
        }

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
