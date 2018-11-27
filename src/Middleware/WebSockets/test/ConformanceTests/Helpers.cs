using System;
using System.IO;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest
{
    public class Helpers
    {
        public static string GetApplicationPath(string projectName)
        {
            var applicationBasePath = AppContext.BaseDirectory;
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "test", projectName));
        }
    }
}
