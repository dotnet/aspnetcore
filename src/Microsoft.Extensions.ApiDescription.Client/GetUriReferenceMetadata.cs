using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GenerationTasks
{
    /// <summary>
    /// Adds or corrects DocumentPath metadata in ServiceUriReference items.
    /// </summary>
    public class GetUriReferenceMetadata : Task
    {
        /// <summary>
        /// Default directory for DocumentPath metadata values.
        /// </summary>
        [Required]
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
                newItem.SetMetadata("DocumentPath", documentPath);
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
