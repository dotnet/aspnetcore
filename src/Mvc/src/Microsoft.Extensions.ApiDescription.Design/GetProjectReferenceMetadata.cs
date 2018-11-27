// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Tasks
{
    /// <summary>
    /// Adds or corrects DocumentPath and project-related metadata in ServiceProjectReference items. Also stores final
    /// metadata as SerializedMetadata.
    /// </summary>
    public class GetProjectReferenceMetadata : Task
    {
        /// <summary>
        /// Default directory for DocumentPath values.
        /// </summary>
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
            var destinations = new HashSet<string>();

            foreach (var item in Inputs)
            {
                var newItem = new TaskItem(item);
                outputs.Add(newItem);

                var documentGenerator = item.GetMetadata("DocumentGenerator");
                if (string.IsNullOrEmpty(documentGenerator))
                {
                    // This case occurs when user overrides the default metadata.
                    Log.LogError(Resources.FormatInvalidEmptyMetadataValue(
                        "DocumentGenerator",
                        "ServiceProjectReference",
                        item.ItemSpec));
                }

                var documentPath = item.GetMetadata("DocumentPath");
                if (string.IsNullOrEmpty(documentPath))
                {
                    var filename = item.GetMetadata("Filename");
                    var documentName = item.GetMetadata("DocumentName");
                    if (string.IsNullOrEmpty(documentName))
                    {
                        documentName = "v1";
                    }

                    documentPath = $"{filename}.{documentName}.json";
                }

                documentPath = GetFullPath(documentPath);
                MetadataSerializer.SetMetadata(newItem, "DocumentPath", documentPath);

                if (!destinations.Add(documentPath))
                {
                    // This case may occur when user is experimenting e.g. with multiple generators or options.
                    // May also occur when user accidentally duplicates DocumentPath metadata.
                    Log.LogError(Resources.FormatDuplicateProjectDocumentPaths(documentPath));
                }

                // Add metadata which may be used as a property and passed to an inner build.
                newItem.SetMetadata("SerializedMetadata", MetadataSerializer.SerializeMetadata(newItem));
            }

            Outputs = outputs.ToArray();

            return !Log.HasLoggedErrors;
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
