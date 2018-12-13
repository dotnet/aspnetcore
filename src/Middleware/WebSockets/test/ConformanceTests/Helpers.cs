using System;
using System.IO;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest
{
    public class Helpers
    {
        public static string GetApplicationPath(string projectName)
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, projectName));
        }
    }
}
