// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Tasks
{
    /// <summary>
    /// Adds or corrects ClassName, Namespace and OutputPath metadata in ServiceFileReference items. Also stores final
    /// metadata as SerializedMetadata.
    /// </summary>
    public class GetFileReferenceMetadata : Task
    {
        private const string TypeScriptLanguageName = "TypeScript";

        /// <summary>
        /// Extension to use in default OutputPath metadata value. Ignored when generating TypeScript.
        /// </summary>
        [Required]
        public string Extension { get; set; }

        /// <summary>
        /// Default Namespace metadata value.
        /// </summary>
        [Required]
        public string Namespace { get; set; }

        /// <summary>
        /// Default directory for OutputPath values.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// The ServiceFileReference items to update.
        /// </summary>
        [Required]
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// The updated ServiceFileReference items. Will include ClassName, Namespace and OutputPath metadata.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs{ get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var outputs = new List<ITaskItem>(Inputs.Length);
            var destinations = new HashSet<string>();

            foreach (var item in Inputs)
            {
                var newItem = new TaskItem(item);
                outputs.Add(newItem);

                var codeGenerator = item.GetMetadata("CodeGenerator");
                if (string.IsNullOrEmpty("CodeGenerator"))
                {
                    // This case occurs when user forgets to specify the required metadata. We have no default here.
                    string type;
                    if (!string.IsNullOrEmpty(item.GetMetadata("SourceProject")))
                    {
                        type = "ServiceProjectReference";
                    }
                    else if (!string.IsNullOrEmpty(item.GetMetadata("SourceUri")))
                    {
                        type = "ServiceUriReference";
                    }
                    else
                    {
                        type = "ServiceFileReference";
                    }

                    Log.LogError(Resources.FormatInvalidEmptyMetadataValue("CodeGenerator", type, item.ItemSpec));
                }

                var className = item.GetMetadata("ClassName");
                if (string.IsNullOrEmpty(className))
                {
                    var filename = item.GetMetadata("Filename");
                    className = $"{filename}Client";
                    if (char.IsLower(className[0]))
                    {
                        className = char.ToUpper(className[0]) + className.Substring(startIndex: 1);
                    }

                    MetadataSerializer.SetMetadata(newItem, "ClassName", className);
                }

                var @namespace = item.GetMetadata("Namespace");
                if (string.IsNullOrEmpty(@namespace))
                {
                    MetadataSerializer.SetMetadata(newItem, "Namespace", Namespace);
                }

                var outputPath = item.GetMetadata("OutputPath");
                if (string.IsNullOrEmpty(outputPath))
                {
                    var isTypeScript = codeGenerator.EndsWith(TypeScriptLanguageName, StringComparison.OrdinalIgnoreCase);
                    outputPath = $"{className}{(isTypeScript ? ".ts" : Extension)}";
                }

                // Place output file in correct directory (relative to project directory).
                if (!Path.IsPathRooted(outputPath) && !string.IsNullOrEmpty(OutputDirectory))
                {
                    outputPath = Path.Combine(OutputDirectory, outputPath);
                }

                if (!destinations.Add(outputPath))
                {
                    // This case may occur when user is experimenting e.g. with multiple code generators or options.
                    // May also occur when user accidentally duplicates OutputPath metadata.
                    Log.LogError(Resources.FormatDuplicateFileOutputPaths(outputPath));
                }

                MetadataSerializer.SetMetadata(newItem, "OutputPath", outputPath);

                // Add metadata which may be used as a property and passed to an inner build.
                newItem.RemoveMetadata("SerializedMetadata");
                newItem.SetMetadata("SerializedMetadata", MetadataSerializer.SerializeMetadata(newItem));
            }

            Outputs = outputs.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
