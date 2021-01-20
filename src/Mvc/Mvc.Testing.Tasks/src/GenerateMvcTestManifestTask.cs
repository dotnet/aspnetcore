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
    /// <summary>
    /// Generate a JSON file mapping assemblies to content root paths.
    /// </summary>
    public class GenerateMvcTestManifestTask : Task
    {
        /// <summary>
        /// The path to output the manifest file to.
        /// </summary>
        [Required]
        public string ManifestPath { get; set; }

        /// <summary>
        /// The path to copy deps files to.
        /// </summary>
        public string PathToCopyDeps { get; set; }

        /// <summary>
        /// A list of content root paths and assembly names to generate the
        /// manifest from.
        /// </summary>
        [Required]
        public ITaskItem[] Projects { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            using var fileStream = File.Create(ManifestPath);
            var output = new Dictionary<string, string>();
            foreach (var project in Projects)
            {
                var contentRoot = project.GetMetadata("ContentRoot");
                var assemblyName = project.GetMetadata("Identity");

                // This is only set when publishing
                if (string.IsNullOrEmpty(PathToCopyDeps))
                {
                    output[assemblyName] = contentRoot;
                }
                else
                {
                    // When publishing content root is always the BaseDirectory
                    output[assemblyName] = "~";
                    var depsFile = project.GetMetadata("DepsFile");
                    Log.LogMessage("Looking for " + depsFile + ": "+ File.Exists(depsFile));
                    if (File.Exists(depsFile))
                    {
                        File.Copy(depsFile, Path.Combine(PathToCopyDeps, Path.GetFileName(depsFile)));
                    }
                }
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
