// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Client
{
    /// <summary>
    /// Adds or corrects Namespace and OutputPath metadata in ServiceFileReference items.
    /// </summary>
    public class GetFileReferenceMetadata : Task
    {
        /// <summary>
        /// Default Namespace metadata value for C# output.
        /// </summary>
        [Required]
        public string CSharpNamespace { get; set; }

        /// <summary>
        /// Default directory for OutputPath values.
        /// </summary>
        [Required]
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Default Namespace metadata value for TypeScript output.
        /// </summary>
        [Required]
        public string TypeScriptNamespace { get; set; }

        /// <summary>
        /// The ServiceFileReference items to update.
        /// </summary>
        [Required]
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// The updated ServiceFileReference items. Will include Namespace and OutputPath metadata. OutputPath metadata
        /// will contain full paths.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs{ get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var outputs = new List<ITaskItem>(Inputs.Length);
            foreach (var item in Inputs)
            {
                var newItem = new TaskItem(item);
                outputs.Add(newItem);

                var codeGenerator = item.GetMetadata("CodeGenerator");
                var isTypeScript = codeGenerator.EndsWith("TypeScript", StringComparison.OrdinalIgnoreCase);

                var @namespace = item.GetMetadata("Namespace");
                if (string.IsNullOrEmpty(@namespace))
                {
                    @namespace = isTypeScript ? CSharpNamespace : TypeScriptNamespace;
                    MetadataSerializer.SetMetadata(newItem, "Namespace", @namespace);
                }

                var outputPath = item.GetMetadata("OutputPath");
                if (string.IsNullOrEmpty(outputPath))
                {
                    var className = item.GetMetadata("ClassName");
                    outputPath = $"{className}{(isTypeScript ? ".ts" : ".cs")}";
                }

                outputPath = GetFullPath(outputPath);
                MetadataSerializer.SetMetadata(newItem, "OutputPath", outputPath);

                // Add metadata which may be used as a property and passed to an inner build.
                newItem.SetMetadata("SerializedMetadata", MetadataSerializer.SerializeMetadata(newItem));
            }

            Outputs = outputs.ToArray();

            return true;
        }

        private string GetFullPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                if (!string.IsNullOrEmpty(OutputDirectory))
                {
                    path = Path.Combine(OutputDirectory, path);
                }

                path = Path.GetFullPath(path);
            }

            return path;
        }
    }
}
