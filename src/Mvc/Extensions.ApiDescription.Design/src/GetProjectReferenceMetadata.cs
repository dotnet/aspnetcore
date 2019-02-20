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
    /// Adds or corrects DocumentPath and project-related metadata in ServiceProjectReference items. Also stores final
    /// metadata as SerializedMetadata.
    /// </summary>
    public class GetProjectReferenceMetadata : Task
    {
        private static readonly char[] InvalidFilenameCharacters = Path.GetInvalidFileNameChars();
        private static readonly string[] InvalidFilenameStrings = new[] { ".." };

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

                var documentName = item.GetMetadata("DocumentName");
                if (string.IsNullOrEmpty(documentName))
                {
                    documentName = "v1";
                    MetadataSerializer.SetMetadata(newItem, "DocumentName", documentName);
                }

                var documentPath = item.GetMetadata("DocumentPath");
                if (string.IsNullOrEmpty(documentPath))
                {
                    // No need to sanitize the filename since the project file exists.
                    var projectFilename = item.GetMetadata("Filename");

                    // Default document filename matches project filename unless given a non-default document name.
                    if (string.IsNullOrEmpty(documentName))
                    {
                        // This is an odd (but allowed) case that would break the sanitize one-liner below. Also,
                        // ensure chosen name does not match the "v1" case.
                        documentPath = projectFilename + "_.json";
                    }
                    else if (string.Equals("v1", documentName, StringComparison.Ordinal))
                    {
                        documentPath = projectFilename + ".json";
                    }
                    else
                    {
                        // Sanitize the document name because it may contain almost any character, including illegal
                        // filename characters such as '/' and '?'. (Do not treat slashes as folder separators.)
                        var sanitizedDocumentName = string.Join("_", documentName.Split(InvalidFilenameCharacters));
                        while (sanitizedDocumentName.Contains(InvalidFilenameStrings[0]))
                        {
                            sanitizedDocumentName = string.Join(
                                ".",
                                sanitizedDocumentName.Split(InvalidFilenameStrings, StringSplitOptions.None));
                        }

                        documentPath = $"{projectFilename}_{sanitizedDocumentName}";

                        // Possible the document path already ends with .json. Don't duplicate that or a final period.
                        if (!documentPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            if (documentPath.EndsWith(".", StringComparison.Ordinal))
                            {
                                documentPath += "json";
                            }
                            else
                            {
                                documentPath += ".json";
                            }
                        }
                    }
                }

                documentPath = GetFullPath(documentPath);
                if (!destinations.Add(documentPath))
                {
                    // This case may occur when user is experimenting e.g. with multiple generators or options.
                    // May also occur when user accidentally duplicates DocumentPath metadata.
                    Log.LogError(Resources.FormatDuplicateProjectDocumentPaths(documentPath));
                }

                MetadataSerializer.SetMetadata(newItem, "DocumentPath", documentPath);

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
