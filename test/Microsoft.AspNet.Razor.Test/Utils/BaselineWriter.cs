using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNet.Razor.Test.Utils
{
    public static class BaselineWriter
    {
        [Conditional("GENERATE_BASELINES")]
        public static void WriteBaseline(string baselineFile, string output)
        {
            var root = RecursiveFind("Razor.sln", Path.GetFullPath("."));
            var baselinePath = Path.Combine(root, baselineFile);

            // Update baseline
            // IMPORTANT! Replace this path with the local path on your machine to the baseline files!
            if (File.Exists(baselinePath))
            {
                File.Delete(baselinePath);
            }
            File.WriteAllText(baselinePath, output.ToString());
        }

        private static string RecursiveFind(string path, string start)
        {
            var test = Path.Combine(start, path);
            if (File.Exists(test))
            {
                return start;
            }
            else
            {
                return RecursiveFind(path, new DirectoryInfo(start).Parent.FullName);
            }
        }
    }
}
