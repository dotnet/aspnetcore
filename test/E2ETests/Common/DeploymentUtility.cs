using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace E2ETests
{
    internal class DeploymentUtility
    {
        private static string GetIISExpressPath(RuntimeArchitecture architecture)
        {
            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS Express", "iisexpress.exe");

            // Get path to 64 bit of IIS Express
            if (architecture == RuntimeArchitecture.amd64)
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
            var aspNetLoaderDestPath = Path.Combine(applicationPath, "wwwroot", "bin", "AspNet.Loader.dll");

            // Create bin directory if it does not exist.
            if (!Directory.Exists(new DirectoryInfo(aspNetLoaderDestPath).Parent.FullName))
            {
                Directory.CreateDirectory(new DirectoryInfo(aspNetLoaderDestPath).Parent.FullName);
            }

            if (!File.Exists(aspNetLoaderDestPath))
            {
                File.Copy(aspNetLoaderSrcPath, aspNetLoaderDestPath);
            }
        }

        private static string APP_RELATIVE_PATH = Path.Combine("..", "..", "src", "MusicStore");

        public static Process StartApplication(StartParameters startParameters, string identityDbName, ILogger logger)
        {
            startParameters.ApplicationPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, APP_RELATIVE_PATH));

            //To avoid the KRE_DEFAULT_LIB of the test process flowing into Helios, set it to empty
            var backupRuntimeDefaultLibPath = Environment.GetEnvironmentVariable("KRE_DEFAULT_LIB");
            Environment.SetEnvironmentVariable("KRE_DEFAULT_LIB", string.Empty);

            if (!string.IsNullOrWhiteSpace(startParameters.EnvironmentName))
            {
                if (startParameters.ServerType != ServerType.IISNativeModule &&
                    startParameters.ServerType != ServerType.IIS)
                {
                    // To choose an environment based Startup. 
                    Environment.SetEnvironmentVariable("ASPNET_ENV", startParameters.EnvironmentName);
                }
                else
                {
                    // Cannot override with environment in case of IIS. Bundle and write a Microsoft.AspNet.Hosting.ini file.
                    startParameters.BundleApplicationBeforeStart = true;
                }
            }

            Process hostProcess = null;

            if (startParameters.RuntimeFlavor == RuntimeFlavor.Mono)
            {
                hostProcess = StartMonoHost(startParameters, logger);
            }
            else
            {
                //Tweak the %PATH% to the point to the right RUNTIMEFLAVOR
                startParameters.Runtime = SwitchPathToRuntimeFlavor(startParameters.RuntimeFlavor, startParameters.RuntimeArchitecture, logger);

                //Reason to do pack here instead of in a common place is use the right runtime to do the packing. Previous line switches to use the right runtime.
                if (startParameters.BundleApplicationBeforeStart)
                {
                    if (startParameters.ServerType == ServerType.IISNativeModule ||
                        startParameters.ServerType == ServerType.IIS)
                    {
                        // Bundle to IIS root\application folder.
                        KpmBundle(startParameters, logger, Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + @"\", @"inetpub\wwwroot"));

                        // Drop a Microsoft.AspNet.Hosting.ini with ASPNET_ENV information.
                        logger.WriteInformation("Creating Microsoft.AspNet.Hosting.ini file with ASPNET_ENV.");
                        var iniFile = Path.Combine(startParameters.ApplicationPath, "Microsoft.AspNet.Hosting.ini");
                        File.WriteAllText(iniFile, string.Format("ASPNET_ENV={0}", startParameters.EnvironmentName));

                        // Can't use localdb with IIS. Setting an override to use InMemoryStore.
                        logger.WriteInformation("Creating configoverride.json file to override default config.");
                        var overrideConfig = Path.Combine(startParameters.ApplicationPath, "..", "approot", "src", "MusicStore", "configoverride.json");
                        overrideConfig = Path.GetFullPath(overrideConfig);
                        File.WriteAllText(overrideConfig, "{\"UseInMemoryStore\": \"true\"}");

                        if (startParameters.ServerType == ServerType.IISNativeModule)
                        {
                            logger.WriteInformation("Turning runAllManagedModulesForAllRequests=true in web.config.");
                            // Set runAllManagedModulesForAllRequests=true
                            var webConfig = Path.Combine(startParameters.ApplicationPath, "web.config");
                            var configuration = new XmlDocument();
                            configuration.LoadXml(File.ReadAllText(webConfig));

                            // https://github.com/aspnet/Helios/issues/77
                            var rammfarAttribute = configuration.CreateAttribute("runAllManagedModulesForAllRequests");
                            rammfarAttribute.Value = "true";
                            var modulesNode = configuration.CreateElement("modules");
                            modulesNode.Attributes.Append(rammfarAttribute);
                            var systemWebServerNode = configuration.CreateElement("system.webServer");
                            systemWebServerNode.AppendChild(modulesNode);
                            configuration.SelectSingleNode("//configuration").AppendChild(systemWebServerNode);
                            configuration.Save(webConfig);
                        }

                        logger.WriteInformation("Successfully finished IIS application directory setup.");

                        Thread.Sleep(1 * 1000);
                    }
                    else
                    {
                        KpmBundle(startParameters, logger);
                    }
                }

                if (startParameters.ServerType == ServerType.IISNativeModule ||
                    startParameters.ServerType == ServerType.IIS)
                {
                    startParameters.IISApplication = new IISApplication(startParameters, logger);
                    startParameters.IISApplication.SetupApplication();
                }
                else if (startParameters.ServerType == ServerType.IISExpress)
                {
                    hostProcess = StartHeliosHost(startParameters, logger);
                }
                else
                {
                    hostProcess = StartSelfHost(startParameters, identityDbName, logger);
                }
            }

            //Restore the KRE_DEFAULT_LIB after starting the host process
            Environment.SetEnvironmentVariable("KRE_DEFAULT_LIB", backupRuntimeDefaultLibPath);
            Environment.SetEnvironmentVariable("ASPNET_ENV", string.Empty);
            return hostProcess;
        }

        private static Process StartMonoHost(StartParameters startParameters, ILogger logger)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var runtimeBin = path.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).
                Where(c => c.Contains("kre-mono")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(runtimeBin))
            {
                throw new Exception("Runtime not detected on the machine.");
            }

            if (startParameters.BundleApplicationBeforeStart)
            {
                // We use full path to runtime to pack.
                startParameters.Runtime = new DirectoryInfo(runtimeBin).Parent.FullName;
                KpmBundle(startParameters, logger);
            }

            //Mono now supports --appbase 
            Environment.SetEnvironmentVariable("KRE_APPBASE", startParameters.ApplicationPath);
            logger.WriteInformation("Setting the --appbase to", startParameters.ApplicationPath);

            var bootstrapper = "klr";

            var commandName = startParameters.ServerType == ServerType.Kestrel ? "kestrel" : string.Empty;
            logger.WriteInformation(string.Format("Executing command: {0} {1} {2}", bootstrapper, startParameters.ApplicationPath, commandName));

            var startInfo = new ProcessStartInfo
            {
                FileName = bootstrapper,
                Arguments = string.Format("{0} {1}", startParameters.ApplicationPath, commandName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true
            };

            var hostProcess = Process.Start(startInfo);
            logger.WriteInformation("Started {0}. Process Id : {1}", hostProcess.MainModule.FileName, hostProcess.Id);
            Thread.Sleep(25 * 1000);

            return hostProcess;
        }

        private static Process StartHeliosHost(StartParameters startParameters, ILogger logger)
        {
            if (!string.IsNullOrWhiteSpace(startParameters.ApplicationHostConfigTemplateContent))
            {
                startParameters.ApplicationHostConfigTemplateContent =
                    startParameters.ApplicationHostConfigTemplateContent.Replace("[ApplicationPhysicalPath]", Path.Combine(startParameters.ApplicationPath, "wwwroot"));
            }

            CopyAspNetLoader(startParameters.ApplicationPath);

            if (!string.IsNullOrWhiteSpace(startParameters.ApplicationHostConfigTemplateContent))
            {
                //Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
                //We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

                var tempApplicationHostConfig = Path.GetTempFileName();
                File.WriteAllText(tempApplicationHostConfig, startParameters.ApplicationHostConfigTemplateContent.Replace("[ApplicationPhysicalPath]", startParameters.ApplicationPath));
                startParameters.ApplicationHostConfigLocation = tempApplicationHostConfig;
            }

            var webroot = startParameters.ApplicationPath;
            if (!webroot.EndsWith("wwwroot"))
            {
                webroot = Path.Combine(webroot, "wwwroot");
            }

            var parameters = string.IsNullOrWhiteSpace(startParameters.ApplicationHostConfigLocation) ?
                            string.Format("/port:5001 /path:{0}", webroot) :
                            string.Format("/site:{0} /config:{1}", startParameters.SiteName, startParameters.ApplicationHostConfigLocation);

            var iisExpressPath = GetIISExpressPath(startParameters.RuntimeArchitecture);

            logger.WriteInformation("Executing command : {0} {1}", iisExpressPath, parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = parameters,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            logger.WriteInformation("Started iisexpress. Process Id : {0}", hostProcess.Id);

            return hostProcess;
        }

        private static Process StartSelfHost(StartParameters startParameters, string identityDbName, ILogger logger)
        {
            var commandName = startParameters.ServerType == ServerType.WebListener ? "web" : "kestrel";
            logger.WriteInformation("Executing klr.exe --appbase {0} \"Microsoft.Framework.ApplicationHost\" {1}", startParameters.ApplicationPath, commandName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "klr.exe",
                Arguments = string.Format("--appbase {0} \"Microsoft.Framework.ApplicationHost\" {1}", startParameters.ApplicationPath, commandName),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            //Sometimes reading MainModule returns null if called immediately after starting process.
            Thread.Sleep(1 * 1000);

            try
            {
                logger.WriteInformation("Started {0}. Process Id : {1}", hostProcess.MainModule.FileName, hostProcess.Id);
            }
            catch (Win32Exception win32Exception)
            {
                logger.WriteWarning("Cannot access 64 bit modules from a 32 bit process. Failed with following message.", win32Exception);
            }

            WaitTillDbCreated(identityDbName, logger);

            return hostProcess;
        }

        private static string SwitchPathToRuntimeFlavor(RuntimeFlavor runtimeFlavor, RuntimeArchitecture runtimeArchitecture, ILogger logger)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            logger.WriteInformation(string.Empty);
            logger.WriteInformation("Current %PATH% value : {0}", pathValue);

            var replaceStr = new StringBuilder().
                Append("kre").
                Append((runtimeFlavor == RuntimeFlavor.CoreClr) ? "-coreclr" : "-clr").
                Append("-win").
                Append((runtimeArchitecture == RuntimeArchitecture.x86) ? "-x86" : "-x64").
                ToString();

            pathValue = Regex.Replace(pathValue, "kre-(clr|coreclr)-win-(x86|x64)", replaceStr, RegexOptions.IgnoreCase);

            var startIndex = pathValue.IndexOf(replaceStr); // First instance of this runtime name.
            var runtimeName = pathValue.Substring(startIndex, pathValue.IndexOf(';', startIndex) - startIndex);
            runtimeName = runtimeName.Substring(0, runtimeName.IndexOf('\\')); // Trim the \bin from the path.

            // Tweak the %PATH% to the point to the right RUNTIMEFLAVOR.
            Environment.SetEnvironmentVariable("PATH", pathValue);

            logger.WriteInformation(string.Empty);
            logger.WriteInformation("Changing to use runtime : {0}", runtimeName);
            return runtimeName;
        }

        private static void KpmBundle(StartParameters startParameters, ILogger logger, string bundleRoot = null)
        {
            startParameters.BundledApplicationRootPath = Path.Combine(bundleRoot ?? Path.GetTempPath(), Guid.NewGuid().ToString());

            var parameters = string.Format("bundle {0} -o {1} --runtime {2}", startParameters.ApplicationPath, startParameters.BundledApplicationRootPath, startParameters.Runtime);
            logger.WriteInformation("Executing command kpm {0}", parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = "kpm",
                Arguments = parameters,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            hostProcess.WaitForExit(60 * 1000);

            startParameters.ApplicationPath =
                (startParameters.ServerType == ServerType.IISExpress ||
                startParameters.ServerType == ServerType.IISNativeModule ||
                startParameters.ServerType == ServerType.IIS) ?
                Path.Combine(startParameters.BundledApplicationRootPath, "wwwroot") :
                Path.Combine(startParameters.BundledApplicationRootPath, "approot", "src", "MusicStore");

            logger.WriteInformation("kpm bundle finished with exit code : {0}", hostProcess.ExitCode);
        }

        //In case of self-host application activation happens immediately unlike iis where activation happens on first request.
        //So in self-host case, we need a way to block the first request until the application is initialized. In MusicStore application's case, 
        //identity DB creation is pretty much the last step of application setup. So waiting on this event will help us wait efficiently.
        private static void WaitTillDbCreated(string identityDbName, ILogger logger)
        {
            var identityDBFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), identityDbName + ".mdf");
            if (File.Exists(identityDBFullPath))
            {
                logger.WriteWarning("Database file '{0}' exists. Proceeding with the tests.", identityDBFullPath);
                return;
            }

            logger.WriteInformation("Watching for the DB file '{0}'", identityDBFullPath);
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
                        logger.WriteInformation("Database file created '{0}'. Proceeding with the tests.", identityDBFullPath);
                    }
                    else
                    {
                        logger.WriteWarning("Database file '{0}' not created", identityDBFullPath);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.WriteWarning("Received this exception while watching for Database file", exception);
            }
            finally
            {
                dbWatch.Dispose();
            }
        }

        public static void CleanUpApplication(StartParameters startParameters, Process hostProcess, string musicStoreDbName, ILogger logger)
        {
            if (startParameters.ServerType == ServerType.IISNativeModule ||
                startParameters.ServerType == ServerType.IIS)
            {
                // Stop & delete the application pool.
                if (startParameters.IISApplication != null)
                {
                    startParameters.IISApplication.StopAndDeleteAppPool();
                }
            }
            else if (hostProcess != null && !hostProcess.HasExited)
            {
                //Shutdown the host process
                hostProcess.Kill();
                hostProcess.WaitForExit(5 * 1000);
                if (!hostProcess.HasExited)
                {
                    logger.WriteWarning("Unable to terminate the host process with process Id '{0}", hostProcess.Id);
                }
                else
                {
                    logger.WriteInformation("Successfully terminated host process with process Id '{0}'", hostProcess.Id);
                }
            }
            else
            {
                logger.WriteWarning("Host process already exited or never started successfully.");
            }

            if (!Helpers.RunningOnMono)
            {
                //Mono uses InMemoryStore
                DbUtils.DropDatabase(musicStoreDbName, logger);
            }

            if (!string.IsNullOrWhiteSpace(startParameters.ApplicationHostConfigLocation))
            {
                //Delete the temp applicationHostConfig that we created
                if (File.Exists(startParameters.ApplicationHostConfigLocation))
                {
                    try
                    {
                        File.Delete(startParameters.ApplicationHostConfigLocation);
                    }
                    catch (Exception exception)
                    {
                        //Ignore delete failures - just write a log
                        logger.WriteWarning("Failed to delete '{0}'. Exception : {1}", startParameters.ApplicationHostConfigLocation, exception.Message);
                    }
                }
            }

            if (startParameters.BundleApplicationBeforeStart)
            {
                try
                {
                    //We've originally bundled the application in a temp folder. We need to delete it. 
                    Directory.Delete(startParameters.BundledApplicationRootPath, true);
                }
                catch (Exception exception)
                {
                    logger.WriteWarning("Failed to delete directory.", exception);
                }
            }
        }
    }
}