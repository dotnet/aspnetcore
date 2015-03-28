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

namespace E2ETests
{
    internal class DeploymentUtility
    {
        private static string APP_RELATIVE_PATH = Path.Combine("..", "..", "src", "MusicStore");

        public static Process StartApplication(StartParameters startParameters, ILogger logger)
        {
            startParameters.ApplicationPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), APP_RELATIVE_PATH));

            //To avoid the DNX_DEFAULT_LIB of the test process flowing into Helios, set it to empty
            var backupRuntimeDefaultLibPath = Environment.GetEnvironmentVariable("DNX_DEFAULT_LIB");
            Environment.SetEnvironmentVariable("DNX_DEFAULT_LIB", string.Empty);

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
                    // Cannot override with environment in case of IIS. Publish and write a Microsoft.AspNet.Hosting.ini file.
                    startParameters.PublishApplicationBeforeStart = true;
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
                if (startParameters.PublishApplicationBeforeStart)
                {
#if DNX451
                    if (startParameters.ServerType == ServerType.IISNativeModule ||
                        startParameters.ServerType == ServerType.IIS)
                    {
                        // Publish to IIS root\application folder.
                        DnuPublish(startParameters, logger, Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + @"\", @"inetpub\wwwroot"));

                        // Drop a Microsoft.AspNet.Hosting.ini with ASPNET_ENV information.
                        logger.LogInformation("Creating Microsoft.AspNet.Hosting.ini file with ASPNET_ENV.");
                        var iniFile = Path.Combine(startParameters.ApplicationPath, "Microsoft.AspNet.Hosting.ini");
                        File.WriteAllText(iniFile, string.Format("ASPNET_ENV={0}", startParameters.EnvironmentName));

                        // Can't use localdb with IIS. Setting an override to use InMemoryStore.
                        logger.LogInformation("Creating configoverride.json file to override default config.");
                        var overrideConfig = Path.Combine(startParameters.ApplicationPath, "..", "approot", "src", "MusicStore", "configoverride.json");
                        overrideConfig = Path.GetFullPath(overrideConfig);
                        File.WriteAllText(overrideConfig, "{\"UseInMemoryStore\": \"true\"}");

                        if (startParameters.ServerType == ServerType.IISNativeModule)
                        {
                            logger.LogInformation("Turning runAllManagedModulesForAllRequests=true in web.config.");
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

                        logger.LogInformation("Successfully finished IIS application directory setup.");

                        Thread.Sleep(1 * 1000);
                    }
                    else
#endif
                    {
                        DnuPublish(startParameters, logger);
                    }
                }

#if DNX451
                if (startParameters.ServerType == ServerType.IISNativeModule ||
                    startParameters.ServerType == ServerType.IIS)
                {
                    startParameters.IISApplication = new IISApplication(startParameters, logger);
                    startParameters.IISApplication.SetupApplication();
                }
                else
#endif
                if (startParameters.ServerType == ServerType.IISExpress)
                {
                    hostProcess = StartHeliosHost(startParameters, logger);
                }
                else
                {
                    hostProcess = StartSelfHost(startParameters, logger);
                }
            }

            //Restore the DNX_DEFAULT_LIB after starting the host process
            Environment.SetEnvironmentVariable("DNX_DEFAULT_LIB", backupRuntimeDefaultLibPath);
            Environment.SetEnvironmentVariable("ASPNET_ENV", string.Empty);
            return hostProcess;
        }

        private static Process StartMonoHost(StartParameters startParameters, ILogger logger)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var runtimeBin = path.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).
                Where(c => c.Contains("dnx-mono")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(runtimeBin))
            {
                throw new Exception("Runtime not detected on the machine.");
            }

            if (startParameters.PublishApplicationBeforeStart)
            {
                // We use full path to runtime to pack.
                startParameters.Runtime = new DirectoryInfo(runtimeBin).Parent.FullName;
                DnuPublish(startParameters, logger);
            }

            //Mono now supports --appbase 
            logger.LogInformation("Setting the --appbase to {0}", startParameters.ApplicationPath);

            var bootstrapper = "dnx";

            var commandName = startParameters.ServerType == ServerType.Kestrel ? "kestrel" : string.Empty;
            logger.LogInformation("Executing command: {dnx} \"{appPath}\" {command}", bootstrapper, startParameters.ApplicationPath, commandName);

            var startInfo = new ProcessStartInfo
            {
                FileName = bootstrapper,
                Arguments = string.Format("\"{0}\" {1}", startParameters.ApplicationPath, commandName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true
            };

            var hostProcess = Process.Start(startInfo);
            logger.LogInformation("Started {0}. Process Id : {1}", hostProcess.MainModule.FileName, hostProcess.Id);

            if (hostProcess.HasExited)
            {
                logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            return hostProcess;
        }

        private static Process StartHeliosHost(StartParameters startParameters, ILogger logger)
        {
            if (!string.IsNullOrWhiteSpace(startParameters.ApplicationHostConfigTemplateContent))
            {
                startParameters.ApplicationHostConfigTemplateContent =
                    startParameters.ApplicationHostConfigTemplateContent.Replace("[ApplicationPhysicalPath]", Path.Combine(startParameters.ApplicationPath, "wwwroot"));
            }

            if (!startParameters.PublishApplicationBeforeStart)
            {
                IISExpressHelper.CopyAspNetLoader(startParameters.ApplicationPath);
            }

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
                            string.Format("/port:5001 /path:\"{0}\"", webroot) :
                            string.Format("/site:{0} /config:{1}", startParameters.SiteName, startParameters.ApplicationHostConfigLocation);

            var iisExpressPath = IISExpressHelper.GetPath(startParameters.RuntimeArchitecture);

            logger.LogInformation("Executing command : {iisExpress} {args}", iisExpressPath, parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = parameters,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            logger.LogInformation("Started iisexpress. Process Id : {processId}", hostProcess.Id);

            return hostProcess;
        }

        private static Process StartSelfHost(StartParameters startParameters, ILogger logger)
        {
            var commandName = startParameters.ServerType == ServerType.WebListener ? "web" : "kestrel";
            logger.LogInformation("Executing dnx.exe --appbase {appPath} \"Microsoft.Framework.ApplicationHost\" {command}", startParameters.ApplicationPath, commandName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dnx.exe",
                Arguments = string.Format("--appbase \"{0}\" \"Microsoft.Framework.ApplicationHost\" {1}", startParameters.ApplicationPath, commandName),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            //Sometimes reading MainModule returns null if called immediately after starting process.
            Thread.Sleep(1 * 1000);

            if (hostProcess.HasExited)
            {
                logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            try
            {
                logger.LogInformation("Started {fileName}. Process Id : {processId}", hostProcess.MainModule.FileName, hostProcess.Id);
            }
            catch (Win32Exception win32Exception)
            {
                logger.LogWarning("Cannot access 64 bit modules from a 32 bit process. Failed with following message.", win32Exception);
            }

            return hostProcess;
        }

        private static string SwitchPathToRuntimeFlavor(RuntimeFlavor runtimeFlavor, RuntimeArchitecture runtimeArchitecture, ILogger logger)
        {
            var runtimePath = Process.GetCurrentProcess().MainModule.FileName;
            logger.LogInformation(string.Empty);
            logger.LogInformation("Current runtime path is : {0}", runtimePath);

            var replaceStr = new StringBuilder().
                Append("dnx").
                Append((runtimeFlavor == RuntimeFlavor.CoreClr) ? "-coreclr" : "-clr").
                Append("-win").
                Append((runtimeArchitecture == RuntimeArchitecture.x86) ? "-x86" : "-x64").
                ToString();

            runtimePath = Regex.Replace(runtimePath, "dnx-(clr|coreclr)-win-(x86|x64)", replaceStr, RegexOptions.IgnoreCase);
            runtimePath = Path.GetDirectoryName(runtimePath);

            // Tweak the %PATH% to the point to the right RUNTIMEFLAVOR.
            Environment.SetEnvironmentVariable("PATH", runtimePath + ";" + Environment.GetEnvironmentVariable("PATH"));

            var runtimeDirectoryInfo = new DirectoryInfo(runtimePath);
            if (!runtimeDirectoryInfo.Exists)
            {
                throw new Exception(
                    string.Format("Requested runtime at location '{0}' does not exist. Please make sure it is installed before running test.",
                    runtimeDirectoryInfo.FullName));
            }

            var runtimeName = runtimeDirectoryInfo.Parent.Name;
            logger.LogInformation(string.Empty);
            logger.LogInformation("Changing to use runtime : {runtimeName}", runtimeName);
            return runtimeName;
        }

        private static void DnuPublish(StartParameters startParameters, ILogger logger, string publishRoot = null)
        {
            startParameters.PublishedApplicationRootPath = Path.Combine(publishRoot ?? Path.GetTempPath(), Guid.NewGuid().ToString());

            var parameters =
                string.Format(
                    "publish {0} -o {1} --runtime {2} {3}",
                    startParameters.ApplicationPath,
                    startParameters.PublishedApplicationRootPath,
                    startParameters.Runtime,
                    startParameters.PublishWithNoSource ? "--no-source" : string.Empty);

            logger.LogInformation("Executing command dnu {args}", parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dnu",
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
                Path.Combine(startParameters.PublishedApplicationRootPath, "wwwroot") :
                Path.Combine(startParameters.PublishedApplicationRootPath, "approot", "src", "MusicStore");

            logger.LogInformation("dnu publish finished with exit code : {exitCode}", hostProcess.ExitCode);
        }

        public static void CleanUpApplication(StartParameters startParameters, Process hostProcess, string musicStoreDbName, ILogger logger)
        {
            if (startParameters.ServerType == ServerType.IISNativeModule ||
                startParameters.ServerType == ServerType.IIS)
            {
#if DNX451
                // Stop & delete the application pool.
                if (startParameters.IISApplication != null)
                {
                    startParameters.IISApplication.StopAndDeleteAppPool();
                }
#endif
            }
            else if (hostProcess != null && !hostProcess.HasExited)
            {
                //Shutdown the host process
                hostProcess.Kill();
                hostProcess.WaitForExit(5 * 1000);
                if (!hostProcess.HasExited)
                {
                    logger.LogWarning("Unable to terminate the host process with process Id '{processId}", hostProcess.Id);
                }
                else
                {
                    logger.LogInformation("Successfully terminated host process with process Id '{processId}'", hostProcess.Id);
                }
            }
            else
            {
                logger.LogWarning("Host process already exited or never started successfully.");
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
                        logger.LogWarning("Failed to delete '{config}'. Exception : {exception}", startParameters.ApplicationHostConfigLocation, exception.Message);
                    }
                }
            }

            if (startParameters.PublishApplicationBeforeStart)
            {
                try
                {
                    //We've originally published the application in a temp folder. We need to delete it. 
                    Directory.Delete(startParameters.PublishedApplicationRootPath, true);
                }
                catch (Exception exception)
                {
                    logger.LogWarning("Failed to delete directory.", exception);
                }
            }
        }
    }
}