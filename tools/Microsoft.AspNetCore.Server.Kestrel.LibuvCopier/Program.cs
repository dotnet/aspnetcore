using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Server.Kestrel.LibuvCopier
{
    public class Program
    {
        public void Main(string[] args)
        {
            try
            {
                var packagesFolder = Environment.GetEnvironmentVariable("DNX_PACKAGES") ??
                                     Path.Combine(GetHome(), ".nuget", "packages");

                packagesFolder = Environment.ExpandEnvironmentVariables(packagesFolder);

                var lockJson = JObject.Parse(File.ReadAllText("project.lock.json"));

                foreach (var libuvLib in lockJson["libraries"].OfType<JProperty>().Where(
                    p => p.Name.StartsWith("Microsoft.AspNetCore.Internal.libuv", StringComparison.Ordinal)))
                {
                    foreach (var filePath in libuvLib.Value["files"].Select(v => v.Value<string>()))
                    {
                        if (filePath.ToString().StartsWith("runtimes/", StringComparison.Ordinal))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            File.Copy(Path.Combine(packagesFolder, libuvLib.Name, filePath), filePath, overwrite: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        // Copied from DNX's DnuEnvironment.cs
        private string GetHome()
        {
#if DNX451
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#else
            var runtimeEnv = Extensions.PlatformAbstractions.PlatformServices.Default.Runtime;
            if (runtimeEnv.OperatingSystem == "Windows")
            {
                return Environment.GetEnvironmentVariable("USERPROFILE") ??
                    Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");

                if (string.IsNullOrEmpty(home))
                {
                    throw new Exception("Home directory not found. The HOME environment variable is not set.");
                }

                return home;
            }
#endif
        }
    }
}
