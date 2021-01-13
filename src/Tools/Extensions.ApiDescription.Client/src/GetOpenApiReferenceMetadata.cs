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
    /// Adds or corrects ClassName, FirstForGenerator, Namespace, and OutputPath metadata in OpenApiReference items.
    /// Also stores final metadata as SerializedMetadata.
    /// </summary>
    public class GetOpenApiReferenceMetadata : Task
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
        /// The OpenApiReference items to update.
        /// </summary>
        [Required]
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// The updated OpenApiReference items. Will include ClassName, Namespace and OutputPath metadata.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs{ get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var outputs = new List<ITaskItem>(Inputs.Length);
            var codeGenerators = new HashSet<string>();
            var destinations = new HashSet<string>();

            foreach (var item in Inputs)
            {
                var codeGenerator = item.GetMetadata("CodeGenerator");
                if (string.IsNullOrEmpty(codeGenerator))
                {
                    // This case occurs when user overrides the required metadata with an empty string.
                    var type = string.IsNullOrEmpty(item.GetMetadata("SourceProject")) ?
                        "OpenApiReference" :
                        "OpenApiProjectReference";

                    Log.LogError(Resources.FormatInvalidEmptyMetadataValue("CodeGenerator", type, item.ItemSpec));
                    continue;
                }

                var newItem = new TaskItem(item);
                outputs.Add(newItem);

                if (codeGenerators.Add(codeGenerator))
                {
                    newItem.SetMetadata("FirstForGenerator", "true");
                }
                else
                {
                    newItem.SetMetadata("FirstForGenerator", "false");
                }

                var outputPath = item.GetMetadata("OutputPath");
                if (string.IsNullOrEmpty(outputPath))
                {
                    // No need to further sanitize this path because the file must exist.
                    var filename = item.GetMetadata("Filename");
                    var isTypeScript = codeGenerator.EndsWith(
                        TypeScriptLanguageName,
                        StringComparison.OrdinalIgnoreCase);

                    outputPath = $"{filename}Client{(isTypeScript ? ".ts" : Extension)}";
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
                    continue;
                }

                MetadataSerializer.SetMetadata(newItem, "OutputPath", outputPath);

                var className = item.GetMetadata("ClassName");
                if (string.IsNullOrEmpty(className))
                {
                    var outputFilename = Path.GetFileNameWithoutExtension(outputPath);

                    className = CSharpIdentifier.SanitizeIdentifier(outputFilename);
                    MetadataSerializer.SetMetadata(newItem, "ClassName", className);
                }

                var @namespace = item.GetMetadata("Namespace");
                if (string.IsNullOrEmpty(@namespace))
                {
                    MetadataSerializer.SetMetadata(newItem, "Namespace", Namespace);
                }

                // Add metadata which may be used as a property and passed to an inner build.
                newItem.RemoveMetadata("SerializedMetadata");
                newItem.SetMetadata("SerializedMetadata", MetadataSerializer.SerializeMetadata(newItem));
            }

            Outputs = outputs.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
