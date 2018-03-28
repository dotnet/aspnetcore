// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestTasks
{
    public class InjectRequestHandler : Task
    {
        [Required]
        public string DepsFile { get; set; }

        [Required]
        public string Rid { get; set; }

        [Required]
        public string LibraryLocation { get; set; }

        public override bool Execute()
        {
            InjectNativeLibrary(DepsFile);

            // Parse input
            return true;
        }

        private void InjectNativeLibrary(string depsFile)
        {
            JToken deps;
            using (var file = File.OpenText(depsFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                deps = JObject.ReadFrom(reader);
            }

            var libraryName = "ANCMRH/1.0";
            var libraries = (JObject)deps["libraries"];

            var target = (JObject)((JObject)deps["targets"]).Properties().First().Value;
            var targetLibrary = target.Properties().FirstOrDefault(p => p.Name == libraryName);
            targetLibrary?.Remove();
            targetLibrary =
                new JProperty(libraryName, new JObject(
                    new JProperty("runtimeTargets", new JObject(
                        new JProperty(LibraryLocation.Replace('\\', '/'), new JObject(
                            new JProperty("rid", Rid),
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
