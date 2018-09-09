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
    /// Adds or corrects DocumentPath and project-related metadata in ServiceProjectReference items.
    /// </summary>
    public class GetProjectReferenceMetadata : Task
    {
        /// <summary>
        /// Default directory for DocumentPath values.
        /// </summary>
        [Required]
        public string DocumentDirectory { get; set; }

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

                var outputPath = item.GetMetadata("OutputPath");
                if (string.IsNullOrEmpty(outputPath))
                {
                    var className = item.GetMetadata("ClassName");
                    outputPath = className + (isTypeScript ? ".ts" : ".cs");
                }

                outputPath = GetFullPath(outputPath);
                newItem.SetMetadata("OutputPath", outputPath);
            }

            Outputs = outputs.ToArray();

            return true;
        }

        private string GetFullPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                if (!string.IsNullOrEmpty(DocumentDirectory))
                {
                    path = Path.Combine(DocumentDirectory, path);
                }

                path = Path.GetFullPath(path);
            }

            return path;
        }
    }
}
