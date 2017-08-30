using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DependencyUpdater
{
    public class UpdateAspNetReference : Task
    {
        private static string[] ProjectFileExtensions = new[]
        {
            ".csproj",
            ".fsproj"
        };

        public string NupkgFile { get; set; }
        public string AspNetVersion { get; set; }
        public string OutDir { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(AspNetVersion))
            {
                throw new ArgumentException($"No value specified for {nameof(AspNetVersion)}.");
            }

            // We can't modify the .nupkg in place because the build system still
            // has a lock on the file. We can read it, but not write it. So copy
            // to the output location and then modify the copy.
            var outFile = Path.Combine(OutDir, Path.GetFileName(NupkgFile));
            File.Copy(NupkgFile, outFile, true);
            
            using (var zipFile = ZipFile.Open(outFile, ZipArchiveMode.Update))
            {
                foreach (var projectFile in zipFile.Entries.Where(IsProjectFile))
                {
                    PerformVariableSubstitution(projectFile);
                }
            }

            return true;
        }

        private static bool IsProjectFile(ZipArchiveEntry entry)
        {
            return ProjectFileExtensions.Any(
                extension => Path.GetExtension(entry.Name).Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private void PerformVariableSubstitution(ZipArchiveEntry entry)
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

        private string SubstituteVariables(string projectFileContents)
        {
            // Currently we only need a way of updating ASP.NET package
            // reference versions, so that's all this does. In the future,
            // we could generalise this into a system for injecting
            // versions for all packages based on the KoreBuild lineup.
            return projectFileContents.Replace(
                "$(TemplateAspNetCoreVersion)",
                AspNetVersion);
        }
    }
}
