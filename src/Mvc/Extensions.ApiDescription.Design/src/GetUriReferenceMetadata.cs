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
    /// Adds or corrects DocumentPath metadata in ServiceUriReference items.
    /// </summary>
    public class GetUriReferenceMetadata : Task
    {
        /// <summary>
        /// Default directory for DocumentPath metadata values.
        /// </summary>
        public string DocumentDirectory { get; set; }

        /// <summary>
        /// The ServiceUriReference items to update.
        /// </summary>
        [Required]
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// The updated ServiceUriReference items. Will include DocumentPath metadata with full paths.
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

                var documentPath = item.GetMetadata("DocumentPath");
                if (string.IsNullOrEmpty(documentPath))
                {
                    var uri = item.ItemSpec;
                    var builder = new UriBuilder(uri);
                    if (!builder.Uri.IsAbsoluteUri)
                    {
                        Log.LogError($"{nameof(Inputs)} item '{uri}' is not an absolute URI.");
                        return false;
                    }

                    if (!string.Equals(Uri.UriSchemeHttp, builder.Scheme, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(Uri.UriSchemeHttps, builder.Scheme, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError($"{nameof(Inputs)} item '{uri}' does not have scheme {Uri.UriSchemeHttp} or " +
                            $"{Uri.UriSchemeHttps}.");
                        return false;
                    }

                    var host = builder.Host
                      .Replace("/", string.Empty)
                      .Replace("[", string.Empty)
                      .Replace("]", string.Empty)
                      .Replace(':', '_');
                    var path = builder.Path
                      .Replace("!", string.Empty)
                      .Replace("'", string.Empty)
                      .Replace("$", string.Empty)
                      .Replace("%", string.Empty)
                      .Replace("&", string.Empty)
                      .Replace("(", string.Empty)
                      .Replace(")", string.Empty)
                      .Replace("*", string.Empty)
                      .Replace("@", string.Empty)
                      .Replace("~", string.Empty)
                      .Replace('/', '_')
                      .Replace(':', '_')
                      .Replace(';', '_')
                      .Replace('+', '_')
                      .Replace('=', '_');

                    documentPath = host + path;
                    if (char.IsLower(documentPath[0]))
                    {
                        documentPath = char.ToUpper(documentPath[0]) + documentPath.Substring(startIndex: 1);
                    }

                    if (!documentPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        documentPath = $"{documentPath}.json";
                    }
                }

                documentPath = GetFullPath(documentPath);
                MetadataSerializer.SetMetadata(newItem, "DocumentPath", documentPath);

                if (!destinations.Add(documentPath))
                {
                    // This case may occur when user is experimenting e.g. with multiple code generators or options.
                    // May also occur when user accidentally duplicates DocumentPath metadata.
                    Log.LogError(Resources.FormatDuplicateUriDocumentPaths(documentPath));
                }
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
