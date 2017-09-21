// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RepoTasks
{
    public class TrimDeps : Task
    {
        [Required]
        public ITaskItem[] DepsFiles { get; set; }

        public override bool Execute()
        {
            foreach (var depsFile in DepsFiles)
            {
                ChangeEntryPointLibraryName(depsFile.GetMetadata("Identity"));
            }

            // Parse input
            return true;
        }


        private void ChangeEntryPointLibraryName(string depsFile)
        {
            JToken deps;
            using (var file = File.OpenText(depsFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                deps = JObject.ReadFrom(reader);
            }

            foreach (JProperty target in deps["targets"])
            {
                var targetLibrary = target.Value.Children<JProperty>().FirstOrDefault();
                if (targetLibrary == null)
                {
                    continue;
                }

                targetLibrary.Remove();
            }

            var library = deps["libraries"].Children<JProperty>().First();
            library.Remove();

            using (var file = File.CreateText(depsFile))
            using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
            {
                deps.WriteTo(writer);
            }
        }
    }
}
