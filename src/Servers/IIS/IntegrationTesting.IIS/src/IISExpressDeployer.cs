// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    /// <summary>
    /// Deployment helper for IISExpress.
    /// </summary>
    public class IISExpressDeployer : IISDeployerBase
    {
        private const string IISExpressRunningMessage = "IIS Express is running.";
        private const string FailedToInitializeBindingsMessage = "Failed to initialize site bindings";
        private const string UnableToStartIISExpressMessage = "Unable to start iisexpress.";
        private const int MaximumAttempts = 5;
        private readonly TimeSpan ShutdownTimeSpan = Debugger.IsAttached ? TimeSpan.FromMinutes(60) : TimeSpan.FromMinutes(1);
        private static readonly Regex UrlDetectorRegex = new Regex(@"^\s*Successfully registered URL ""(?<url>[^""]+)"" for site.*$");

        private Process _hostProcess;

        public IISExpressDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(new IISDeploymentParameters(deploymentParameters), loggerFactory)
        {
        }

        public IISExpressDeployer(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deployment"))
            {
                // Start timer
                StartTimer();

                // For an unpublished application the dllroot points pre-built dlls like projectdir/bin/debug/netcoreappX.Y/
                // and contentRoot points to the project directory so you get things like static assets.
                // For a published app both point to the publish directory.
                var dllRoot = CheckIfPublishIsRequired();
                var contentRoot = string.Empty;
                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                    contentRoot = DeploymentParameters.PublishedApplicationRootPath;
                }
                else
                {
                    // Core+Standalone always publishes. This must be Clr+Standalone or Core+Portable.
                    // Update processPath and arguments for our current scenario
                    contentRoot = DeploymentParameters.ApplicationPath;

                    var executableExtension = DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll" : ".exe";
                    var entryPoint = Path.Combine(dllRoot, DeploymentParameters.ApplicationName + executableExtension);

                    var executableName = string.Empty;
                    var executableArgs = string.Empty;

                    if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
                    {
                        executableName = GetDotNetExeForArchitecture();
                        executableArgs = entryPoint;
                    }
                    else
                    {
                        executableName = entryPoint;
                    }

                    Logger.LogInformation("Executing: {exe} {args}", executableName, executableArgs);
                    DeploymentParameters.EnvironmentVariables["LAUNCHER_PATH"] = executableName;
                    DeploymentParameters.EnvironmentVariables["LAUNCHER_ARGS"] = executableArgs;

                    // CurrentDirectory will point to bin/{config}/{tfm}, but the config and static files aren't copied, point to the app base instead.
                    Logger.LogInformation("ContentRoot: {path}", DeploymentParameters.ApplicationPath);
                    DeploymentParameters.EnvironmentVariables["ASPNETCORE_CONTENTROOT"] = DeploymentParameters.ApplicationPath;
                }

                RunWebConfigActions(contentRoot);


                // Launch the host process.
                var (actualUri, hostExitToken) = await StartIISExpressAsync(contentRoot);

                Logger.LogInformation("Application ready at URL: {appUrl}", actualUri);

                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.

                return new IISDeploymentResult(
                    LoggerFactory,
                    IISDeploymentParameters,
                    applicationBaseUri: actualUri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: hostExitToken,
                    hostProcess: _hostProcess);
            }
        }

        private string CheckIfPublishIsRequired()
        {
            string dllRoot = null;
            var targetFramework = DeploymentParameters.TargetFramework;
            if (!string.IsNullOrEmpty(DeploymentParameters.ApplicationPath))
            {
                // IISIntegration uses this layout
                dllRoot = Path.Combine(DeploymentParameters.ApplicationPath, "bin", DeploymentParameters.RuntimeArchitecture.ToString(),
                    DeploymentParameters.Configuration, targetFramework);

                if (!Directory.Exists(dllRoot))
                {
                    // Most repos use this layout
                    dllRoot = Path.Combine(DeploymentParameters.ApplicationPath, "bin", DeploymentParameters.Configuration, targetFramework);

                    if (!Directory.Exists(dllRoot))
                    {
                        // The bits we need weren't pre-compiled, compile on publish
                        DeploymentParameters.PublishApplicationBeforeDeployment = true;
                    }
                    else if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr
                             && DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86)
                    {
                        // x64 is the default. Publish to rebuild for the right bitness
                        DeploymentParameters.PublishApplicationBeforeDeployment = true;
                    }
                }
            }

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                    && DeploymentParameters.ApplicationType == ApplicationType.Standalone)
            {
                // Publish is always required to get the correct standalone files in the output directory
                DeploymentParameters.PublishApplicationBeforeDeployment = true;
            }

            return dllRoot;
        }

        private async Task<(Uri url, CancellationToken hostExitToken)> StartIISExpressAsync(string contentRoot)
        {
            using (Logger.BeginScope("StartIISExpress"))
            {
                var iisExpressPath = GetIISExpressPath();

                for (var attempt = 0; attempt < MaximumAttempts; attempt++)
                {
                    var uri = TestUriHelper.BuildTestUri(ServerType.IISExpress, DeploymentParameters.ApplicationBaseUriHint);
                    var port = uri.Port;
                    if (port == 0)
                    {
                        port = (uri.Scheme == "https") ? TestPortHelper.GetNextSSLPort() : TestPortHelper.GetNextPort();
                    }

                    Logger.LogInformation("Attempting to start IIS Express on port: {port}", port);
                    PrepareConfig(contentRoot, port);

                    var parameters = string.IsNullOrEmpty(DeploymentParameters.ServerConfigLocation) ?
                                    string.Format("/port:{0} /path:\"{1}\" /trace:error /systray:false", uri.Port, contentRoot) :
                                    string.Format("/site:{0} /config:{1} /trace:error /systray:false", DeploymentParameters.SiteName, DeploymentParameters.ServerConfigLocation);

                    Logger.LogInformation("Executing command : {iisExpress} {parameters}", iisExpressPath, parameters);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = iisExpressPath,
                        Arguments = parameters,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        // VS sets current directory to C:\Program Files\IIS Express
                        WorkingDirectory = Path.GetDirectoryName(iisExpressPath)
                    };

                    AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

                    Uri url = null;
                    var started = new TaskCompletionSource<bool>();

                    var process = new Process() { StartInfo = startInfo };
                    process.OutputDataReceived += (sender, dataArgs) =>
                    {
                        if (string.Equals(dataArgs.Data, UnableToStartIISExpressMessage))
                        {
                            // We completely failed to start and we don't really know why
                            started.TrySetException(new InvalidOperationException("Failed to start IIS Express"));
                        }
                        else if (string.Equals(dataArgs.Data, FailedToInitializeBindingsMessage))
                        {
                            started.TrySetResult(false);
                        }
                        else if (string.Equals(dataArgs.Data, IISExpressRunningMessage))
                        {
                            started.TrySetResult(true);
                        }
                        else if (!string.IsNullOrEmpty(dataArgs.Data))
                        {
                            var m = UrlDetectorRegex.Match(dataArgs.Data);
                            if (m.Success)
                            {
                                url = new Uri(m.Groups["url"].Value);
                            }
                        }
                    };

                    process.EnableRaisingEvents = true;
                    var hostExitTokenSource = new CancellationTokenSource();
                    process.Exited += (sender, e) =>
                    {
                        Logger.LogInformation("iisexpress Process {pid} shut down", process.Id);

                        // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                        started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {process.ExitCode}"));

                        TriggerHostShutdown(hostExitTokenSource);
                    };
                    process.StartAndCaptureOutAndErrToLogger("iisexpress", Logger);
                    Logger.LogInformation("iisexpress Process {pid} started", process.Id);

                    if (process.HasExited)
                    {
                        Logger.LogError("Host process {processName} {pid} exited with code {exitCode} or failed to start.", startInfo.FileName, process.Id, process.ExitCode);
                        throw new Exception("Failed to start host");
                    }

                    // Wait for the app to start
                    // The timeout here is large, because we don't know how long the test could need
                    // We cover a lot of error cases above, but I want to make sure we eventually give up and don't hang the build
                    // just in case we missed one -anurse
                    if (!await started.Task.TimeoutAfter(TimeSpan.FromMinutes(10)))
                    {
                        Logger.LogInformation("iisexpress Process {pid} failed to bind to port {port}, trying again", process.Id, port);

                        // Wait for the process to exit and try again
                        process.WaitForExit(30 * 1000);
                        await Task.Delay(1000); // Wait a second to make sure the socket is completely cleaned up
                    }
                    else
                    {
                        _hostProcess = process;

                        // Ensure iisexpress.exe is killed if test process termination is non-graceful.
                        // Prevents locked files when stop debugging unit test.
                        ProcessTracker.Add(_hostProcess);

                        // cache the process start time for verifying log file name.
                        var _ = _hostProcess.StartTime;

                        Logger.LogInformation("Started iisexpress successfully. Process Id : {processId}, Port: {port}", _hostProcess.Id, port);
                        return (url: url, hostExitToken: hostExitTokenSource.Token);
                    }
                }

                var message = $"Failed to initialize IIS Express after {MaximumAttempts} attempts to select a port";
                Logger.LogError(message);
                throw new TimeoutException(message);
            }
        }

        private void PrepareConfig(string contentRoot, int port)
        {
            var serverConfig = DeploymentParameters.ServerConfigTemplateContent;
            // Config is required. If not present then fall back to one we carry with us.
            if (string.IsNullOrEmpty(serverConfig))
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream("Microsoft.AspNetCore.Server.IntegrationTesting.IIS.Http.config"))
                using (var reader = new StreamReader(stream))
                {
                    serverConfig = reader.ReadToEnd();
                }
            }

            XDocument config = XDocument.Parse(serverConfig);
            // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
            // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

            config.Root
                .RequiredElement("location")
                .RequiredElement("system.webServer")
                .RequiredElement("modules")
                .GetOrAdd("add", "name", AspNetCoreModuleV2ModuleName);

            ConfigureModuleAndBinding(config.Root, contentRoot, port);

            var webConfigPath = Path.Combine(contentRoot, "web.config");
            if (!DeploymentParameters.PublishApplicationBeforeDeployment && !File.Exists(webConfigPath))
            {
                // The elements normally in the web.config are in the applicationhost.config for unpublished apps.
                AddAspNetCoreElement(config.Root);
            }

            RunServerConfigActions(config.Root, contentRoot);
            serverConfig = config.ToString();

            DeploymentParameters.ServerConfigLocation = Path.GetTempFileName();
            Logger.LogDebug("Saving Config to {configPath}", DeploymentParameters.ServerConfigLocation);

            File.WriteAllText(DeploymentParameters.ServerConfigLocation, serverConfig);
        }

        private void AddAspNetCoreElement(XElement config)
        {
            var aspNetCore = config
                .RequiredElement("system.webServer")
                .GetOrAdd("aspNetCore");

            aspNetCore.SetAttributeValue("hostingModel", DeploymentParameters.HostingModel.ToString());
            aspNetCore.SetAttributeValue("arguments", "%LAUNCHER_ARGS%");
            aspNetCore.SetAttributeValue("processPath", "%LAUNCHER_PATH%");

            var handlers = config
                .RequiredElement("location")
                .RequiredElement("system.webServer")
                .RequiredElement("handlers");

            var aspNetCoreHandler = handlers
                .GetOrAdd("add", "name", "aspNetCore");

            aspNetCoreHandler.SetAttributeValue("path", "*");
            aspNetCoreHandler.SetAttributeValue("verb", "*");
            aspNetCoreHandler.SetAttributeValue("modules", AspNetCoreModuleV2ModuleName);
            aspNetCoreHandler.SetAttributeValue("resourceType", "Unspecified");
            // Make aspNetCore handler first
            aspNetCoreHandler.Remove();
            handlers.AddFirst(aspNetCoreHandler);
        }

        protected override IEnumerable<Action<XElement, string>> GetWebConfigActions()
        {
            if (IISDeploymentParameters.PublishApplicationBeforeDeployment)
            {
                // For published apps, prefer the content in the web.config, but update it.
                yield return WebConfigHelpers.AddOrModifyAspNetCoreSection(
                    key: "hostingModel",
                    value: DeploymentParameters.HostingModel.ToString());

                yield return WebConfigHelpers.AddOrModifyHandlerSection(
                    key: "modules",
                    value: AspNetCoreModuleV2ModuleName);

                // We assume the x64 dotnet.exe is on the path so we need to provide an absolute path for x86 scenarios.
                // Only do it for scenarios that rely on dotnet.exe (Core, portable, etc.).
                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                    && DeploymentParameters.ApplicationType == ApplicationType.Portable
                    && DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
                {
                    var executableName = DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture);
                    if (!File.Exists(executableName))
                    {
                        throw new Exception($"Unable to find '{executableName}'.'");
                    }
                    yield return WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", executableName);
                }
            }

            foreach (var action in base.GetWebConfigActions())
            {
                yield return action;
            }
        }

        private string GetIISExpressPath()
        {
            var programFiles = "Program Files";
            if (DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
            {
                programFiles = "Program Files (x86)";
            }

            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", programFiles, "IIS Express", "iisexpress.exe");

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine: " + iisExpressPath);
            }

            return iisExpressPath;
        }

        public override void Dispose()
        {
            Dispose(gracefulShutdown: false);
        }

        public override void Dispose(bool gracefulShutdown)
        {
            using (Logger.BeginScope("Dispose"))
            {
                if (gracefulShutdown)
                {
                    GracefullyShutdownProcess(_hostProcess);
                }
                else
                {
                    ShutDownIfAnyHostProcess(_hostProcess);
                }

                if (!string.IsNullOrEmpty(DeploymentParameters.ServerConfigLocation)
                    && File.Exists(DeploymentParameters.ServerConfigLocation))
                {
                    // Delete the temp applicationHostConfig that we created.
                    Logger.LogDebug("Deleting applicationHost.config file from {configLocation}", DeploymentParameters.ServerConfigLocation);
                    try
                    {
                        File.Delete(DeploymentParameters.ServerConfigLocation);
                    }
                    catch (Exception exception)
                    {
                        // Ignore delete failures - just write a log.
                        Logger.LogWarning("Failed to delete '{config}'. Exception : {exception}", DeploymentParameters.ServerConfigLocation, exception.Message);
                    }
                }

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    CleanPublishedOutput();
                }

                InvokeUserApplicationCleanup();

                StopTimer();
            }

            // If by this point, the host process is still running (somehow), throw an error.
            // A test failure is better than a silent hang and unknown failure later on
            if (_hostProcess != null && !_hostProcess.HasExited)
            {
                throw new Exception($"iisexpress Process {_hostProcess.Id} failed to shutdown");
            }
        }

        private class WindowsNativeMethods
        {
            internal delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);
            [DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);
            [DllImport("user32.dll")]
            internal static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll")]
            internal static extern bool EnumWindows(EnumWindowProc callback, IntPtr lParam);
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName,int nMaxCount);
        }

        private void SendStopMessageToProcess(int pid)
        {
            var found = false;
            var extraLogging = false;
            var retryCount = 5;

            while (!found && retryCount > 0)
            {
                Logger.LogInformation($"Sending shutdown request to {pid}");

                WindowsNativeMethods.EnumWindows((ptr, param) => {
                    WindowsNativeMethods.GetWindowThreadProcessId(ptr, out var windowProcessId);
                    if (extraLogging)
                    {
                        Logger.LogDebug($"EnumWindow returned {ptr} belonging to {windowProcessId}");
                    }

                    if (pid == windowProcessId)
                    {
                        // 256 is the max length
                        var className = new StringBuilder(256);

                        if (WindowsNativeMethods.GetClassName(ptr, className, className.Capacity) == 0)
                        {
                            throw new InvalidOperationException($"Unable to get window class name: {Marshal.GetLastWin32Error()}");
                        }

                        if (!string.Equals(className.ToString(), "IISEXPRESS", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.LogDebug($"Skipping window {ptr} with class name {className}");
                            // skip windows without IISEXPRESS class
                            return true;
                        }

                        var hWnd = new HandleRef(null, ptr);
                        if (!WindowsNativeMethods.PostMessage(hWnd, 0x12, IntPtr.Zero, IntPtr.Zero))
                        {
                            throw new InvalidOperationException($"Unable to PostMessage to process {pid}. LastError: {Marshal.GetLastWin32Error()}");
                        }

                        found = true;
                        return false;
                    }

                    return true;
                }, IntPtr.Zero);

                if (!found)
                {
                    Thread.Sleep(100);
                }

                // Add extra logging if first try was unsuccessful
                extraLogging = true;
                retryCount--;
            }

            if (!found)
            {
                throw new InvalidOperationException($"Unable to find main window for process {pid}");
            }
        }

        private void GracefullyShutdownProcess(Process hostProcess)
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                // Calling hostProcess.StandardInput.WriteLine("q") with StandardInput redirected
                // for the process does not work when stopping IISExpress
                // Also, hostProcess.CloseMainWindow() doesn't work either.
                // Instead we have to send WM_QUIT to the iisexpress process via pInvokes.
                // See: https://stackoverflow.com/questions/4772092/starting-and-stopping-iis-express-programmatically

                SendStopMessageToProcess(hostProcess.Id);
                if (!hostProcess.WaitForExit((int)ShutdownTimeSpan.TotalMilliseconds))
                {
                    throw new InvalidOperationException($"iisexpress Process {hostProcess.Id} failed to gracefully shutdown.");
                }
                if (hostProcess.ExitCode != 0)
                {
                    Logger.LogWarning($"IISExpress exit code is non-zero after graceful shutdown. Exit code: {hostProcess.ExitCode}");
                    throw new InvalidOperationException($"IISExpress exit code is non-zero after graceful shutdown. Exit code: {hostProcess.ExitCode}.");
                }
            }
            else
            {
                throw new InvalidOperationException($"iisexpress Process {hostProcess?.Id} crashed before shutdown was triggered.");
            }
        }
    }
}
