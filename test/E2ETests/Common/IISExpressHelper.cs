using System;
using System.IO;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace E2ETests
{
    public class IISExpressHelper
    {
        public static string GetPath(RuntimeArchitecture architecture)
        {
            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "IIS Express", "iisexpress.exe");

            // Get path to 64 bit of IIS Express
            if (architecture == RuntimeArchitecture.amd64)
            {
                iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "IIS Express", "iisexpress.exe");

                // If process is 32 bit, the path points to x86. Replace path to point to x64
                iisExpressPath = IntPtr.Size == 8 ? iisExpressPath : iisExpressPath.Replace(" (x86)", "");
            }

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine");
            }

            return iisExpressPath;
        }

        public static void CopyAspNetLoader(string applicationPath)
        {
            var libraryManager = (ILibraryManager)CallContextServiceLocator.Locator.ServiceProvider.GetService(typeof(ILibraryManager));
            var interopLibrary = libraryManager.GetLibraryInformation("Microsoft.AspNet.Loader.IIS.Interop");

            var aspNetLoaderSrcPath = Path.Combine(interopLibrary.Path, "tools", "AspNet.Loader.dll");
            var aspNetLoaderDestPath = Path.Combine(applicationPath, "wwwroot", "bin", "AspNet.Loader.dll");

            // Create bin directory if it does not exist.
            Directory.CreateDirectory(new DirectoryInfo(aspNetLoaderDestPath).Parent.FullName);

            if (!File.Exists(aspNetLoaderDestPath))
            {
                File.Copy(aspNetLoaderSrcPath, aspNetLoaderDestPath);
            }
        }
    }
}