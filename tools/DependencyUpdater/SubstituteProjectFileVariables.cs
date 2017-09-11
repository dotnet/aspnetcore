using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
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

        [Required] public string NupkgFile { get; set; }
        [Required] public string OutDir { get; set; }
        [Required] public ITaskItem[] Substitutions { get; set; }

        public override bool Execute()
        {
            // We can't modify the .nupkg in place because the build system still
            // has a lock on the file. We can read it, but not write it. So copy
            // to the output location and then modify the copy.
            var outFile = Path.Combine(OutDir, Path.GetFileName(NupkgFile));
            File.Copy(NupkgFile, outFile, true);

            var numProjectFiles = 0;
            using (var zipFile = ZipFile.Open(outFile, ZipArchiveMode.Update))
            {
                foreach (var projectFile in zipFile.Entries.Where(IsProjectFile))
                {
                    numProjectFiles++;
                    PerformVariableSubstitutions(projectFile);
                }
            }

            if (numProjectFiles == 0)
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"No project files found in {Path.GetFileName(outFile)}, so no variables substituted.");
            }
            else
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"Substituted variables in {numProjectFiles} project file(s) in {Path.GetFileName(outFile)}");
            }

            return true;
        }

        private static bool IsProjectFile(ZipArchiveEntry entry)
        {
            return ProjectFileExtensions.Any(
                extension => Path.GetExtension(entry.Name).Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private void PerformVariableSubstitutions(ZipArchiveEntry entry)
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
                    writer.Write(SubstituteVariables(contents));
                }
            }
        }

        private string SubstituteVariables(string text)
        {
            foreach (var item in Substitutions)
            {
                text = text.Replace($"$({item.ItemSpec})", item.GetMetadata("Value"));
            }

            return text;
        }
    }
}
