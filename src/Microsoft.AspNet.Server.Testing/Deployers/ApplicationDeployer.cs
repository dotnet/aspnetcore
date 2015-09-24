// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Abstract base class of all deployers with implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IApplicationDeployer
    {
        /// <summary>
        /// Example: runtimes/dnx-coreclr-win-x64.1.0.0-rc1-15844/bin
        /// </summary>
        protected string ChosenRuntimePath { get; set; }

        /// <summary>
        /// Examples: dnx-coreclr-win-x64.1.0.0-rc1-15844, dnx-mono.1.0.0-rc1-15844
        /// </summary>
        protected string ChosenRuntimeName { get; set; }

        protected DeploymentParameters DeploymentParameters { get; private set; }

        protected ILogger Logger { get; private set; }

        protected Stopwatch StopWatch { get; private set; } = new Stopwatch();

        protected string OSPrefix
        {
            get
            {
                if (TestPlatformHelper.IsLinux)
                {
                    return "linux";
                }
                else if (TestPlatformHelper.IsMac)
                {
                    return "darwin";
                }
                else if (TestPlatformHelper.IsWindows)
                {
                    return "win";
                }
                else
                {
                    throw new InvalidOperationException("Unrecognized operating system");
                }
            }
        }

        protected string DnuCommandName
        {
            get
            {
                if (TestPlatformHelper.IsWindows)
                {
                    return "dnu.cmd";
                }
                else
                {
                    return "dnu";
                }
            }
        }

        protected string DnxCommandName
        {
            get
            {
                if (TestPlatformHelper.IsWindows)
                {
                    return "dnx.exe";
                }
                else
                {
                    return "dnx";
                }
            }
        }

        public abstract DeploymentResult Deploy();

        public ApplicationDeployer(
            DeploymentParameters deploymentParameters,
            ILogger logger)
        {
            DeploymentParameters = deploymentParameters;
            Logger = logger;
        }

        protected string PopulateChosenRuntimeInformation()
        {
            // ex: runtimes/dnx-coreclr-win-x64.1.0.0-rc1-15844/bin
            var currentRuntimeBinPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Logger.LogInformation($"Current runtime path is : {currentRuntimeBinPath}");

            var targetRuntimeName = new StringBuilder()
                .Append("dnx")
                .Append((DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr) ? "-coreclr" : "-clr")
                .Append($"-{OSPrefix}")
                .Append((DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86) ? "-x86" : "-x64")
                .ToString();

            string targetRuntimeBinPath;
            // Ex: When current runtime is Mono and the tests are being run for CoreClr
            if (currentRuntimeBinPath.Contains("dnx-mono"))
            {
                targetRuntimeBinPath = currentRuntimeBinPath.Replace("dnx-mono", targetRuntimeName);
            }
            else
            {
                targetRuntimeBinPath = Regex.Replace(
                    currentRuntimeBinPath,
                    "dnx-(clr|coreclr)-(win|linux|darwin)-(x86|x64)",
                    targetRuntimeName,
                    RegexOptions.IgnoreCase);
            }

            var targetRuntimeBinDir = new DirectoryInfo(targetRuntimeBinPath);
            if (targetRuntimeBinDir == null || !targetRuntimeBinDir.Exists)
            {
                throw new Exception($"Requested runtime at location '{targetRuntimeBinPath}' does not exist.Please make sure it is installed before running test.");
            }

            ChosenRuntimePath = targetRuntimeBinDir.FullName;
            ChosenRuntimeName = targetRuntimeBinDir.Parent.Name;
            DeploymentParameters.DnxRuntime = ChosenRuntimeName;

            Logger.LogInformation($"Chosen runtime path is {ChosenRuntimePath}");

            return ChosenRuntimeName;
        }

        protected void DnuPublish(string publishRoot = null)
        {
            DeploymentParameters.PublishedApplicationRootPath = Path.Combine(publishRoot ?? Path.GetTempPath(), Guid.NewGuid().ToString());

            var noSource = DeploymentParameters.PublishWithNoSource ? "--no-source" : string.Empty;
            var command = DeploymentParameters.Command ?? "web";
            var parameters = $"publish {DeploymentParameters.ApplicationPath} -o {DeploymentParameters.PublishedApplicationRootPath}"
                + $" --runtime {DeploymentParameters.DnxRuntime} {noSource} --iis-command {command}";

            var dnuPath = Path.Combine(ChosenRuntimePath, DnuCommandName);
            Logger.LogInformation($"Executing command {dnuPath} {parameters}");

            var startInfo = new ProcessStartInfo
            {
                FileName = dnuPath,
                Arguments = parameters,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var hostProcess = new Process() { StartInfo = startInfo };
            hostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
            hostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
            hostProcess.Start();
            hostProcess.BeginErrorReadLine();
            hostProcess.BeginOutputReadLine();
            hostProcess.WaitForExit();

            if (hostProcess.ExitCode != 0)
            {
                throw new Exception(string.Format("dnu publish exited with exit code : {0}", hostProcess.ExitCode));
            }

            DeploymentParameters.ApplicationPath =
                (DeploymentParameters.ServerType == ServerType.IISExpress ||
                DeploymentParameters.ServerType == ServerType.IIS) ?
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "wwwroot") :
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "approot", "src",
                                new DirectoryInfo(DeploymentParameters.ApplicationPath).Name);

            Logger.LogInformation($"dnu publish finished with exit code : {hostProcess.ExitCode}");
        }

        protected void CleanPublishedOutput()
        {
            try
            {
                // We've originally published the application in a temp folder. We need to delete it.
                Directory.Delete(DeploymentParameters.PublishedApplicationRootPath, true);
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"Failed to delete directory : {exception.Message}");
            }
        }

        protected void ShutDownIfAnyHostProcess(Process hostProcess)
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                // Shutdown the host process.
                hostProcess.Kill();
                hostProcess.WaitForExit(5 * 1000);
                if (!hostProcess.HasExited)
                {
                    Logger.LogWarning("Unable to terminate the host process with process Id '{processId}", hostProcess.Id);
                }
                else
                {
                    Logger.LogInformation("Successfully terminated host process with process Id '{processId}'", hostProcess.Id);
                }
            }
            else
            {
                Logger.LogWarning("Host process already exited or never started successfully.");
            }
        }

        protected void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo)
        {
            var environment =
#if DNX451
                startInfo.EnvironmentVariables;
#elif DNXCORE50
                startInfo.Environment;
#endif

            SetEnvironmentVariable(environment, "ASPNET_ENV", DeploymentParameters.EnvironmentName);

            // Work around for https://github.com/aspnet/dnx/issues/1515
            if (DeploymentParameters.PublishWithNoSource)
            {
                SetEnvironmentVariable(environment, "DNX_PACKAGES", null);
            }

            SetEnvironmentVariable(environment, "DNX_DEFAULT_LIB", null);

            foreach (var environmentVariable in DeploymentParameters.EnvironmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

#if DNX451
        protected void SetEnvironmentVariable(System.Collections.Specialized.StringDictionary environment, string name, string value)
        {
#elif DNXCORE50
        protected void SetEnvironmentVariable(System.Collections.Generic.IDictionary<string, string> environment, string name, string value)
        {
#endif
            if (value == null)
            {
                Logger.LogInformation("Removing environment variable {name}", name);
                environment.Remove(name);
            }
            else
            {
                Logger.LogInformation("SET {name}={value}", name, value);
                environment[name] = value;
            }
        }

        protected void InvokeUserApplicationCleanup()
        {
            if (DeploymentParameters.UserAdditionalCleanup != null)
            {
                // User cleanup.
                try
                {
                    DeploymentParameters.UserAdditionalCleanup(DeploymentParameters);
                }
                catch (Exception exception)
                {
                    Logger.LogWarning("User cleanup code failed with exception : {exception}", exception.Message);
                }
            }
        }

        protected void TriggerHostShutdown(CancellationTokenSource hostShutdownSource)
        {
            Logger.LogInformation("Host process shutting down.");
            try
            {
                hostShutdownSource.Cancel();
            }
            catch (Exception)
            {
                // Suppress errors.
            }
        }

        protected void StartTimer()
        {
            Logger.LogInformation($"Deploying {DeploymentParameters.ToString()}");
            StopWatch.Start();
        }

        protected void StopTimer()
        {
            StopWatch.Stop();
            Logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", StopWatch.Elapsed.TotalSeconds);
        }

        public abstract void Dispose();
    }
}