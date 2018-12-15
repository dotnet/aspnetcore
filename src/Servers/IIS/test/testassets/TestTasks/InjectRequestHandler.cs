// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestTasks
{
    public class InjectRequestHandler
    {
        private static void Main(string[] args)
        {
            string rid = args[0];
            string libraryLocation = args[1];
            string depsFile = args[2];

            JToken deps;
            using (var file = File.OpenText(depsFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                deps = JObject.ReadFrom(reader);
            }

            var libraryName = "ANCMRH/1.0";
            var libraries = (JObject)deps["libraries"];
            var targetName = (JValue)deps["runtimeTarget"]["name"];

            var target = (JObject)deps["targets"][targetName.Value];
            var targetLibrary = target.Properties().FirstOrDefault(p => p.Name == libraryName);
            targetLibrary?.Remove();
            targetLibrary =
                new JProperty(libraryName, new JObject(
                    new JProperty("runtimeTargets", new JObject(
                        new JProperty(libraryLocation.Replace('\\', '/'), new JObject(
                            new JProperty("rid", rid),
                            new JProperty("assetType", "native")
                        ))))));
            target.AddFirst(targetLibrary);

            var library = libraries.Properties().FirstOrDefault(p => p.Name == libraryName);
            library?.Remove();
            library =
                new JProperty(libraryName, new JObject(
                    new JProperty("type", "package"),
                    new JProperty("serviceable", true),
                    new JProperty("sha512", ""),
                    new JProperty("path", libraryName),
                    new JProperty("hashPath", "")));
            libraries.AddFirst(library);
            
            using (var file = File.CreateText(depsFile))
            using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
            {
                deps.WriteTo(writer);
            }
        }
    }
}
