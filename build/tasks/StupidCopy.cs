using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    /// <summary>
    /// Because MSBuild can't copy files that exceed MAX_PATH and
    /// https://github.com/Microsoft/msbuild/issues/53 has been open since 2015.
    /// </summary>
    public class StupidCopy : Task
    {
        public string[] SourceFiles { get; set; }
        public string[] DestinationFiles { get; set; }
        public string DestinationFolder { get; set; }
        public override bool Execute()
        {
            if (SourceFiles == null || SourceFiles.Length == 0)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(DestinationFolder))
            {
                Directory.CreateDirectory(DestinationFolder);

                foreach (var file in SourceFiles)
                {
                    string destFileName = Path.Combine(DestinationFolder, Path.GetFileName(file));
                    Log.LogMessage("Copying {0} to {1}", file, destFileName);
                    File.Copy(file, destFileName, overwrite: true);
                }
            }
            else
            {
                if (SourceFiles.Length != DestinationFiles.Length)
                {
                    Log.LogError("The number of sources and destinations does not match");
                    return false;
                }
                for (int i = 0; i < SourceFiles.Length; i++)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DestinationFiles[i]));
                    Log.LogMessage("Copying {0} to {1}", SourceFiles[i], DestinationFiles[i]);
                    File.Copy(SourceFiles[i], DestinationFiles[i], overwrite: true);
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
