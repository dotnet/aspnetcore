using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace E2ETests
{
    internal class DeploymentUtility
    {
        private static string GetIISExpressPath(KreArchitecture architecture)
        {
            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS Express", "iisexpress.exe");

            // Get path to 64 bit of IIS Express
            if (architecture == KreArchitecture.amd64)
            {
                iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe");

                // If process is 32 bit, the path points to x86. Replace path to point to x64
                iisExpressPath = Environment.Is64BitProcess ? iisExpressPath : iisExpressPath.Replace(" (x86)", "");
            }

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine");
            }

            return iisExpressPath;
        }

        /// <summary>
        /// Copy AspNet.Loader.dll to bin folder
        /// </summary>
        /// <param name="applicationPath"></param>
        private static void CopyAspNetLoader(string applicationPath)
        {
            var libraryManager = (ILibraryManager)CallContextServiceLocator.Locator.ServiceProvider.GetService(typeof(ILibraryManager));
            var interopLibrary = libraryManager.GetLibraryInformation("Microsoft.AspNet.Loader.IIS.Interop");

            var aspNetLoaderSrcPath = Path.Combine(interopLibrary.Path, "tools", "AspNet.Loader.dll");
            var aspNetLoaderDestPath = Path.Combine(applicationPath, "bin", "AspNet.Loader.dll");

            if (!File.Exists(aspNetLoaderDestPath))
            {
                File.Copy(aspNetLoaderSrcPath, aspNetLoaderDestPath);
            }
        }

        private const string APP_RELATIVE_PATH = @"..\..\src\MusicStore\";

        public static Process StartApplication(ServerType hostType, KreFlavor kreFlavor, KreArchitecture kreArchitecture, string identityDbName)
        {
            string applicationPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, APP_RELATIVE_PATH));

            var backupKreDefaultLibPath = Environment.GetEnvironmentVariable("KRE_DEFAULT_LIB");
            //To avoid the KRE_DEFAULT_LIB of the test process flowing into Helios, set it to empty
            Environment.SetEnvironmentVariable("KRE_DEFAULT_LIB", string.Empty);

            Process hostProcess = null;

            if (kreFlavor == KreFlavor.Mono)
            {
                hostProcess = StartMonoHost(hostType, applicationPath);
            }
            else
            {
                //Tweak the %PATH% to the point to the right KREFLAVOR
                Environment.SetEnvironmentVariable("PATH", SwitchPathToKreFlavor(kreFlavor, kreArchitecture));

                if (hostType == ServerType.Helios)
                {
                    hostProcess = StartHeliosHost(applicationPath, kreArchitecture);
                }
                else
                {
                    hostProcess = StartSelfHost(hostType, applicationPath, identityDbName);
                }
            }

            //Restore the KRE_DEFAULT_LIB after starting the host process
            Environment.SetEnvironmentVariable("KRE_DEFAULT_LIB", backupKreDefaultLibPath);
            return hostProcess;
        }

        private static Process StartMonoHost(ServerType hostType, string applicationPath)
        {
            //Mono needs this as GetFullPath does not work if it has \
            applicationPath = Path.GetFullPath(applicationPath.Replace('\\', '/'));

            //Mono does not have a way to pass in a --appbase switch. So it will be an environment variable. 
            Environment.SetEnvironmentVariable("KRE_APPBASE", applicationPath);
            Console.WriteLine("Setting the KRE_APPBASE to {0}", applicationPath);

            var path = Environment.GetEnvironmentVariable("PATH");
            var kreBin = path.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Where(c => c.Contains("KRE-mono45")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(kreBin))
            {
                throw new Exception("KRE not detected on the machine.");
            }

            var monoPath = Path.Combine(kreBin, "mono");
            var klrMonoManaged = Path.Combine(kreBin, "klr.mono.managed.dll");
            var applicationHost = Path.Combine(kreBin, "Microsoft.Framework.ApplicationHost");

            Console.WriteLine(string.Format("Executing command: {0} {1} {3} {4}", monoPath, klrMonoManaged, applicationPath, applicationHost, hostType.ToString()));

            var startInfo = new ProcessStartInfo
            {
                FileName = monoPath,
                Arguments = string.Format("{0} {1} {2}", klrMonoManaged, applicationHost, hostType.ToString()),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            Console.WriteLine("Started {0}. Process Id : {1}", hostProcess.MainModule.FileName, hostProcess.Id);
            Thread.Sleep(5 * 1000);

            //Clear the appbase so that it does not create issues with successive runs
            Environment.SetEnvironmentVariable("KRE_APPBASE", string.Empty);
            return hostProcess;
        }

        private static Process StartHeliosHost(string applicationPath, KreArchitecture kreArchitecture)
        {
            CopyAspNetLoader(applicationPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = GetIISExpressPath(kreArchitecture),
                Arguments = string.Format("/port:5001 /path:{0}", applicationPath),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            Console.WriteLine("Started iisexpress. Process Id : {0}", hostProcess.Id);

            return hostProcess;
        }

        private static Process StartSelfHost(ServerType hostType, string applicationPath, string identityDbName)
        {
            Console.WriteLine(string.Format("Executing klr.exe --appbase {0} \"Microsoft.Framework.ApplicationHost\" {1}", applicationPath, hostType.ToString()));

            var startInfo = new ProcessStartInfo
            {
                FileName = "klr.exe",
                Arguments = string.Format("--appbase {0} \"Microsoft.Framework.ApplicationHost\" {1}", applicationPath, hostType.ToString()),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            //Sometimes reading MainModule returns null if called immediately after starting process.
            Thread.Sleep(1 * 1000);

            try
            {
                Console.WriteLine("Started {0}. Process Id : {1}", hostProcess.MainModule.FileName, hostProcess.Id);
            }
            catch (Win32Exception win32Exception)
            {
                Console.WriteLine("Cannot access 64 bit modules from a 32 bit process. Failed with following message : {0}", win32Exception.Message);
            }

            WaitTillDbCreated(identityDbName);

            return hostProcess;
        }

        private static string SwitchPathToKreFlavor(KreFlavor kreFlavor, KreArchitecture kreArchitecture)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            Console.WriteLine();
            Console.WriteLine("Current %PATH% value : {0}", pathValue);

            StringBuilder replaceStr = new StringBuilder();
            replaceStr.Append("KRE");
            replaceStr.Append((kreFlavor == KreFlavor.CoreClr) ? "-CoreCLR" : "-CLR");
            replaceStr.Append((kreArchitecture == KreArchitecture.x86) ? "-x86" : "-amd64");

            pathValue = Regex.Replace(pathValue, "KRE-(CLR|CoreCLR)-(x86|amd64)", replaceStr.ToString(), RegexOptions.IgnoreCase);

            Console.WriteLine();
            Console.WriteLine("Setting %PATH% value to : {0}", pathValue);
            return pathValue;
        }

        //In case of self-host application activation happens immediately unlike iis where activation happens on first request.
        //So in self-host case, we need a way to block the first request until the application is initialized. In MusicStore application's case, 
        //identity DB creation is pretty much the last step of application setup. So waiting on this event will help us wait efficiently.
        private static void WaitTillDbCreated(string identityDbName)
        {
            var identityDBFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), identityDbName + ".mdf");
            if (File.Exists(identityDBFullPath))
            {
                Console.WriteLine("Database file '{0}' exists. Proceeding with the tests.", identityDBFullPath);
                return;
            }

            Console.WriteLine("Watching for the DB file '{0}'", identityDBFullPath);
            var dbWatch = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), identityDbName + ".mdf");
            dbWatch.EnableRaisingEvents = true;

            try
            {
                if (!File.Exists(identityDBFullPath))
                {
                    //Wait for a maximum of 1 minute assuming the slowest cold start.
                    var watchResult = dbWatch.WaitForChanged(WatcherChangeTypes.Created, 60 * 1000);
                    if (watchResult.ChangeType == WatcherChangeTypes.Created)
                    {
                        //This event is fired immediately after the localdb file is created. Give it a while to finish populating data and start the application.
                        Thread.Sleep(5 * 1000);
                        Console.WriteLine("Database file created '{0}'. Proceeding with the tests.", identityDBFullPath);
                    }
                    else
                    {
                        Console.WriteLine("Database file '{0}' not created", identityDBFullPath);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Received this exception while watching for Database file {0}", exception);
            }
            finally
            {
                dbWatch.Dispose();
            }
        }

        public static void CleanUpApplication(Process hostProcess, string musicStoreDbName)
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                //Shutdown the host process
                hostProcess.Kill();
                hostProcess.WaitForExit(5 * 1000);
                if (!hostProcess.HasExited)
                {
                    Console.WriteLine("Unable to terminate the host process with process Id '{0}", hostProcess.Id);
                }
                else
                {
                    Console.WriteLine("Successfully terminated host process with process Id '{0}'", hostProcess.Id);
                }
            }
            else
            {
                Console.WriteLine("Host process already exited or never started successfully.");
            }

            if (!Helpers.RunningOnMono)
            {
                //Mono uses InMemoryStore
                DbUtils.DropDatabase(musicStoreDbName);
            }
        }
    }
}