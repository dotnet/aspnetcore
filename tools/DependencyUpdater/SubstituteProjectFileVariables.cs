using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DependencyUpdater
{
    public class SubstituteProjectFileVariables : Task
    {
        private static string[] ProjectFileExtensions = new[]
        {
            ".csproj",
            ".fsproj"
        };

        public string NupkgFile { get; set; }
        public string Substitutions { get; set; }
        public string OutDir { get; set; }

        public override bool Execute()
        {
            var substitutionsDict = ParseSubstitutions(Substitutions);

            // We can't modify the .nupkg in place because the build system still
            // has a lock on the file. We can read it, but not write it. So copy
            // to the output location and then modify the copy.
            var outFile = Path.Combine(OutDir, Path.GetFileName(NupkgFile));
            File.Copy(NupkgFile, outFile, true);
            
            using (var zipFile = ZipFile.Open(outFile, ZipArchiveMode.Update))
            {
                foreach (var projectFile in zipFile.Entries.Where(IsProjectFile))
                {
                    PerformVariableSubstitutions(projectFile, substitutionsDict);
                }
            }

            return true;
        }

        private static IDictionary<string, string> ParseSubstitutions(string substitutions)
        {
            // Takes input of the form "key1=val1; key2=val2" (as is common in MSBuild)
            return substitutions.Split(new[] { ';' })
                .Select(pair => pair.Trim())
                .Where(pair => !string.IsNullOrEmpty(pair) && pair.IndexOf('=') > 0)
                .Select(pair => pair.Split('='))
                .ToDictionary(splitPair => splitPair[0].Trim(), splitPair => splitPair[1].Trim());
        }

        private static bool IsProjectFile(ZipArchiveEntry entry)
        {
            return ProjectFileExtensions.Any(
                extension => Path.GetExtension(entry.Name).Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private static void PerformVariableSubstitutions(ZipArchiveEntry entry, IDictionary<string, string> substitutions)
        {
            using (var fileStream = entry.Open())
            {
                string contents;
                using (var reader = new StreamReader(fileStream))
                using (var writer = new StreamWriter(fileStream))
                {
                    contents = reader.ReadToEnd();
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.SetLength(0);
                    writer.Write(SubstituteVariables(contents, substitutions));
                }
            }
        }

        private static string SubstituteVariables(string text, IDictionary<string, string> substitutions)
        {
            foreach (var kvp in substitutions)
            {
                text = text.Replace($"$({kvp.Key})", kvp.Value);
            }

            return text;
        }
    }
}
