using System;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Server.Kestrel.LibuvCopier
{
    public class Program
    {
        private readonly IRuntimeEnvironment _runtimeEnv;

        public Program(IRuntimeEnvironment runtimeEnv)
        {
            _runtimeEnv = runtimeEnv;
        }

        public void Main(string[] args)
        {
            try
            {
                var packagesFolder = Environment.GetEnvironmentVariable("DNX_PACKAGES");

                if (string.IsNullOrEmpty(packagesFolder))
                {
                    var dnxFolder = Environment.GetEnvironmentVariable("DNX_HOME");

                    if (string.IsNullOrEmpty(dnxFolder))
                    {
                        dnxFolder = Path.Combine(GetHome(), ".dnx");
                    }

                    packagesFolder = Path.Combine(dnxFolder, "packages");
                }

                packagesFolder = Environment.ExpandEnvironmentVariables(packagesFolder);

                var lockJson = JObject.Parse(File.ReadAllText("project.lock.json"));

                foreach (var libuvLib in lockJson["libraries"].OfType<JProperty>().Where(
                    p => p.Name.StartsWith("Microsoft.AspNet.Internal.libuv", StringComparison.Ordinal)))
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
            catch(Exception ex)
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
            if (_runtimeEnv.OperatingSystem == "Windows")
            {
                return Environment.GetEnvironmentVariable("USERPROFILE") ??
                    Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            }
            else
            {
                return Environment.GetEnvironmentVariable("HOME");
            }
#endif
        }
    }
}
