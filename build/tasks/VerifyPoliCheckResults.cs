using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class VerifyPoliCheckResults : Task
    {
        /// <summary>
        /// The result of running PoliCheck.
        /// </summary>
        [Required]
        public ITaskItem PoliCheckOutputFile { get; set; }

        public ITaskItem[] DetailedExclusions { get; set; }

        public ITaskItem[] ExcludeFiles { get; set; }

        public string[] ExcludeTerms { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(PoliCheckOutputFile.ItemSpec))
            {
                Log.LogError($"Could not locate file \"{PoliCheckOutputFile.ItemSpec}\".");
                return false;
            }

            var xDocument = XDocument.Load(PoliCheckOutputFile.ItemSpec);

            foreach (var item in DetailedExclusions)
            {
                Log.LogMessage("Exclusion: File={0};Term={1};Line={2}", new object[]
                {
                    item.GetMetadata("FullPath"),
                    item.GetMetadata("Term"),
                    item.GetMetadata("Line")
                });
            }

            var success = true;
            var usedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedExcludedFiles = new HashSet<ITaskItem>();
            var usedDetailedExclusion = new HashSet<ITaskItem>();
            foreach (var result in xDocument.Descendants("Result"))
            {
                foreach (var resultObject in result.Elements("Object"))
                {
                    var term = resultObject.Element("Term").Value;
                    var isExcludedTerm = ExcludeTerms.Any(ex => string.Equals(ex, term, StringComparison.OrdinalIgnoreCase));
                    if (isExcludedTerm)
                    {
                        usedTerms.Add(term);
                        continue;
                    }

                    var filePath = resultObject.Attribute("URL").Value;

                    var excludedFile = ExcludeFiles.FirstOrDefault(ex => filePath.EndsWith(ex.ItemSpec, StringComparison.OrdinalIgnoreCase));
                    if (excludedFile != null)
                    {
                        usedExcludedFiles.Add(excludedFile);
                        continue;
                    }

                    var positionText = resultObject.Element("Position").Value;
                    var lineNumber = 0;
                    if (!string.IsNullOrEmpty(positionText))
                    {
                        int.TryParse(positionText.Substring("Line: ".Length), out lineNumber);
                    }

                    var excludedInDetail = DetailedExclusions.FirstOrDefault(item =>
                    {
                        var exclusionFilePath = item.ItemSpec;
                        var exclusionTerm = item.GetMetadata("Term");

                        return
                            filePath.EndsWith(exclusionFilePath, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(exclusionTerm, term, StringComparison.OrdinalIgnoreCase);

                    });

                    if (excludedInDetail != null)
                    {
                        usedDetailedExclusion.Add(excludedInDetail);
                        continue;
                    }

                    var termClass = resultObject.Element("TermClass").Value;
                    var severity = resultObject.Element("Severity").Value;
                    var contextElement = resultObject.Element("Context");

                    var context = contextElement.Value;
                    var columnText = contextElement.Attribute("TermAt").Value;
                    int.TryParse(columnText, out var column);

                    var message = $"Term [{term}] {context}";
                    Log.LogError($"Policheck {termClass}", $"PC0{severity}", null, filePath, lineNumber, column, 0, 0, message);
                    success = false;
                }
            }

            foreach (var term in ExcludeTerms.Except(usedTerms, StringComparer.OrdinalIgnoreCase))
            {
                Log.LogWarning($"Unused excluded term '{term}'.");
            }

            foreach (var exclusion in ExcludeFiles.Except(usedExcludedFiles))
            {
                Log.LogWarning($"Unused excluded file '{exclusion.ItemSpec}'.");
            }

            foreach (var exclusion in DetailedExclusions.Except(usedDetailedExclusion))
            {
                Log.LogWarning($"Unused exclusion '{exclusion.ItemSpec}' for term '{exclusion.GetMetadata("Term")}'.");
            }

            return success;
        }
    }
}
