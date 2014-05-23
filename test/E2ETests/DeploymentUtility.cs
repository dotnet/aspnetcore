using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace E2ETests
{
    internal class DeploymentUtility
    {
        private static string GetIISExpressPath()
        {
            var iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS Express", "iisexpress.exe");

            //If X86 version does not exist
            if (!File.Exists(iisExpressPath))
            {
                iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe");

                if (!File.Exists(iisExpressPath))
                {
                    throw new Exception("Unable to find IISExpress on the machine");
                }
            }

            return iisExpressPath;
        }

        /// <summary>
        /// Copy AspNet.Loader.dll to bin folder
        /// </summary>
        /// <param name="applicationPath"></param>
        private static void CopyAspNetLoader(string applicationPath)
        {
            string packagesDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\Packages"));
            var aspNetLoaderSrcPath = Path.Combine(Directory.GetDirectories(packagesDirectory, "Microsoft.AspNet.Loader.IIS.Interop.*").First(), @"tools\AspNet.Loader.dll");
            var aspNetLoaderDestPath = Path.Combine(applicationPath, @"bin\AspNet.Loader.dll");
            if (!File.Exists(aspNetLoaderDestPath))
            {
                File.Copy(aspNetLoaderSrcPath, aspNetLoaderDestPath);
            }
        }

        private const string APP_RELATIVE_PATH = @"..\..\src\MusicStore\";

        public static Process StartApplication(HostType hostType, KreFlavor kreFlavor)
        {
            string applicationPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, APP_RELATIVE_PATH));

            if (hostType == HostType.Helios)
            {
                return StartHeliosHost(applicationPath);
            }
            else
            {
                throw new NotImplementedException("Self-Host variation not implemented");
            }
        }

        private static Process StartHeliosHost(string applicationPath)
        {
            CopyAspNetLoader(applicationPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = GetIISExpressPath(),
                Arguments = string.Format("/port:5001 /path:{0}", applicationPath),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            Console.WriteLine("Started iisexpress. Process Id : {0}", hostProcess.Id);
            Thread.Sleep(2 * 1000);

            return hostProcess;
        }

        //private static Process StartSelfHost(string applicationPath)
        //{
        //    var klrPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".kre\packges", "KRE-svr50-x86.0.1-alpha-build-0450", @"bin\klr.exe");
        //    Console.WriteLine(klrPath);

        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = klrPath,
        //        Arguments = string.Format("--appbase {0} \"Microsoft.Framework.ApplicationHost\" web", applicationPath),
        //        UseShellExecute = true,
        //        CreateNoWindow = true
        //    };

        //    var hostProcess = Process.Start(startInfo);
        //    Console.WriteLine("Started klr.exe. Process Id : {0}", hostProcess.Id);
        //    Thread.Sleep(10 * 1000);

        //    return hostProcess;
        //}
    }
}