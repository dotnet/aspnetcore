// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Abstract base class of all deplolyers with a back bone implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IApplicationDeployer
    {
        protected string ChosenRuntimePath { get; set; }

        protected string ChosenRuntimeName { get; set; }

        protected DeploymentParameters DeploymentParameters { get; private set; }

        protected ILogger Logger { get; private set; }

        protected Stopwatch StopWatch { get; private set; } = new Stopwatch();

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
            var runtimePath = Process.GetCurrentProcess().MainModule.FileName;
            Logger.LogInformation(string.Empty);
            Logger.LogInformation("Current runtime path is : {0}", runtimePath);

            var replaceStr = new StringBuilder().
                Append("dnx").
                Append((DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr) ? "-coreclr" : "-clr").
                Append("-win").
                Append((DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86) ? "-x86" : "-x64").
                ToString();

            runtimePath = Regex.Replace(runtimePath, "dnx-(clr|coreclr)-win-(x86|x64)", replaceStr, RegexOptions.IgnoreCase);
            ChosenRuntimePath = Path.GetDirectoryName(runtimePath);

            var runtimeDirectoryInfo = new DirectoryInfo(ChosenRuntimePath);
            if (!runtimeDirectoryInfo.Exists)
            {
                throw new Exception(
                    string.Format("Requested runtime at location '{0}' does not exist. Please make sure it is installed before running test.",
                    runtimeDirectoryInfo.FullName));
            }

            ChosenRuntimeName = runtimeDirectoryInfo.Parent.Name;
            Logger.LogInformation(string.Empty);
            Logger.LogInformation("Changing to use runtime : {runtimeName}", ChosenRuntimeName);
            return ChosenRuntimeName;
        }

        protected void DnuPublish(string publishRoot = null)
        {
            DeploymentParameters.PublishedApplicationRootPath = Path.Combine(publishRoot ?? Path.GetTempPath(), Guid.NewGuid().ToString());

            var parameters =
                string.Format(
                    "publish {0} -o {1} --runtime {2} {3}",
                    DeploymentParameters.ApplicationPath,
                    DeploymentParameters.PublishedApplicationRootPath,
                    DeploymentParameters.DnxRuntime,
                    DeploymentParameters.PublishWithNoSource ? "--no-source" : string.Empty);

            Logger.LogInformation("Executing command dnu {args}", parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(ChosenRuntimePath, "dnu.cmd"),
                Arguments = parameters,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            hostProcess.WaitForExit(60 * 1000);

            DeploymentParameters.ApplicationPath =
                (DeploymentParameters.ServerType == ServerType.IISExpress ||
                DeploymentParameters.ServerType == ServerType.IISNativeModule ||
                DeploymentParameters.ServerType == ServerType.IIS) ?
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "wwwroot") :
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "approot", "src",
                                new DirectoryInfo(DeploymentParameters.ApplicationPath).Name);

            Logger.LogInformation("dnu publish finished with exit code : {exitCode}", hostProcess.ExitCode);
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
                Logger.LogWarning("Failed to delete directory : {error}", exception.Message);
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
            Logger.LogInformation("Deploying {VariationDetails}", DeploymentParameters.ToString());
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