using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace RepoTasks
{
    /// <summary>
    /// Computes the checksum for a single file.
    /// </summary>
    public class GetFileHash : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// The files to be hashed.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The input files with additional metadata set to include the file hash.
        /// </summary>
        [Output]
        public ITaskItem[] Items { get; set; }

        public override bool Execute()
        {
            Parallel.ForEach(Files, file =>
            {
                var hash = ComputeHash(file.ItemSpec);
                file.SetMetadata("FileHash", EncodeHash(hash));
            });

            Items = Files;
            return !Log.HasLoggedErrors;
        }

        internal static string EncodeHash(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }

        internal static byte[] ComputeHash(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(stream);
            }
        }
    }
}
