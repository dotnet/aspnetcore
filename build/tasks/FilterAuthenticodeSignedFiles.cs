using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace RepoTasks
{
    /// <summary>
    /// Determine which files are already signed.
    /// </summary>
    public class FilterAuthenticodeSignedFiles : Microsoft.Build.Utilities.Task
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
                if (WinTrust.IsAuthenticodeSigned(file.ItemSpec))
                {
                    signed.Add(file);
                }
                else
                {
                    unsigned.Add(file);
                }
            });

            Signed = signed.ToArray();
            Unsigned = unsigned.ToArray();
            Log.LogMessage(MessageImportance.High, "Found {0} signed and {1} unsigned files", Signed.Length, Unsigned.Length);
            Debug.Assert(Signed.Length + Unsigned.Length == Files.Length, "Make sure all files are accounted for");
            return !Log.HasLoggedErrors;
        }
    }
}
