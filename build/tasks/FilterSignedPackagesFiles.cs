using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace RepoTasks
{
    /// <summary>
    /// Determine which files are already signed.
    /// </summary>
    public class FilterSignedPackagesFiles : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// The files to be hashed.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The files which are signed.
        /// </summary>
        [Output]
        public ITaskItem[] Signed { get; set; }

        /// <summary>
        /// The files which are not
        /// </summary>
        [Output]
        public ITaskItem[] Unsigned { get; set; }

        public override bool Execute()
        {
            var signed = new ConcurrentBag<ITaskItem>();
            var unsigned = new ConcurrentBag<ITaskItem>();
            Parallel.ForEach(Files, file =>
            {
                if (IsPackageSigned(file.ItemSpec))
                {
                    signed.Add(file);
                }
                else
                {
                    Log.LogMessage(MessageImportance.High, "Package {0} is not signed.", Path.GetFileName(file.ItemSpec));
                    unsigned.Add(file);
                }
            });

            Signed = signed.ToArray();
            Unsigned = unsigned.ToArray();
            Log.LogMessage(MessageImportance.High, "Found {0} signed and {1} unsigned files", Signed.Length, Unsigned.Length);
            Debug.Assert(Signed.Length + Unsigned.Length == Files.Length, "Make sure all files are accounted for");
            return !Log.HasLoggedErrors;
        }

        private bool IsPackageSigned(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                switch (Path.GetExtension(filePath).ToLowerInvariant())
                {
                    case ".nupkg":
                        return zip.GetEntry(".signature.p7s") != null;
                    case ".vsix":
                        return zip.GetEntry("package/services/digital-signature/_rels/origin.psdor.rels") != null;
                    case ".jar":
                        return zip.GetEntry("META-INF/MSFTSIG.RSA") != null;
                    default:
                        Log.LogError("Unrecognized package type: {0}", filePath);
                        return false;
                }
            }
        }
    }
}
