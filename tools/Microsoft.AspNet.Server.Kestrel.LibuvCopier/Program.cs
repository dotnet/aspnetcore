using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Server.Kestrel.LibuvCopier
{
    public class Program
    {
        public void Main(string[] args)
        {
            try
            {
                var packagesFolder = Environment.GetEnvironmentVariable("DNX_PACKAGES");

                if (string.IsNullOrEmpty(packagesFolder))
                {
                    var dnxFolder = Environment.GetEnvironmentVariable("DNX_HOME");

#if DNX451
                    // DNXCore,Version=v5.0 error CS0117: 'Environment' does not contain a definition for 'SpecialFolder'
                    // DNXCore,Version=v5.0 error CS0117: 'Environment' does not contain a definition for 'GetFolderPath'
                    if (string.IsNullOrEmpty(dnxFolder))
                    {
                        dnxFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dnx");
                    }
#endif

                    if (!string.IsNullOrEmpty(dnxFolder))
                    {
                        packagesFolder = Path.Combine(dnxFolder, "packages");
                    }
                    else
                    {
                        throw new Exception("DNX folder not found. Try setting the DNX_HOME and/or DNX_PACKAGES environment variables.");
                    }
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
    }
}
