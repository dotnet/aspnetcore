// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Mvc.Testing.Tasks
{
    public class GenerateMvcTestManifest : Task
    {
        [Required]
        public string ManifestPath { get; set; }

        [Required]
        public ITaskItem[] Projects { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(ManifestPath);
            var output = new Dictionary<string, string>();

            foreach (var project in Projects) {
                var path = project.GetMetadata("Path");
                var assemblyName = project.GetMetadata("Name");
                output[assemblyName] = path;
            }

            var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });
            using var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, ownsStream: false, indent: true);
            serializer.WriteObject(writer, output);

            return !Log.HasLoggedErrors;
        }
    }
}
