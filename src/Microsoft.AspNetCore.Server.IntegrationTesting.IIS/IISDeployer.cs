// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    /// <summary>
    /// Deployer for IIS.
    /// </summary>
    public partial class IISDeployer : IISDeployerBase
    {
        private IISApplication _application;
        private CancellationTokenSource _hostShutdownToken = new CancellationTokenSource();

        public IISDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(new IISDeploymentParameters(deploymentParameters), loggerFactory)
        {
        }

        public IISDeployer(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override void Dispose()
        {
            if (_application != null)
            {
                _application.StopAndDeleteAppPool().GetAwaiter().GetResult();

                TriggerHostShutdown(_hostShutdownToken);
            }

            GetLogsFromFile($"{_application.WebSiteName}.txt");

            CleanPublishedOutput();
            InvokeUserApplicationCleanup();

            StopTimer();
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deployment"))
            {
                StartTimer();

                var contentRoot = string.Empty;
                if (string.IsNullOrEmpty(DeploymentParameters.ServerConfigTemplateContent))
                {
                    DeploymentParameters.ServerConfigTemplateContent = File.ReadAllText("IIS.config");
                }

                _application = new IISApplication(IISDeploymentParameters, Logger);

                // For now, only support using published output
                DeploymentParameters.PublishApplicationBeforeDeployment = true;

                if (DeploymentParameters.ApplicationType == ApplicationType.Portable)
                {
                    DefaultWebConfigActions.Add(
                        WebConfigHelpers.AddOrModifyAspNetCoreSection(
                            "processPath",
                            DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture)));
                }

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                    contentRoot = DeploymentParameters.PublishedApplicationRootPath;
                    // Do not override settings set on parameters
                    if (!IISDeploymentParameters.HandlerSettings.ContainsKey("debugLevel") &&
                        !IISDeploymentParameters.HandlerSettings.ContainsKey("debugFile"))
                    {
                        var logFile = Path.Combine(contentRoot, $"{_application.WebSiteName}.txt");
                        IISDeploymentParameters.HandlerSettings["debugLevel"] = "4";
                        IISDeploymentParameters.HandlerSettings["debugFile"] = logFile;
                    }

                    DefaultWebConfigActions.Add(WebConfigHelpers.AddOrModifyHandlerSection(
                        key: "modules",
                        value: DeploymentParameters.AncmVersion.ToString()));
                    RunWebConfigActions(contentRoot);
                }

                var uri = TestUriHelper.BuildTestUri(ServerType.IIS, DeploymentParameters.ApplicationBaseUriHint);
                // To prevent modifying the IIS setup concurrently.
                await _application.StartIIS(uri, contentRoot);

                // Warm up time for IIS setup.
                Logger.LogInformation("Successfully finished IIS application directory setup.");
                return new IISDeploymentResult(
                    LoggerFactory,
                    IISDeploymentParameters,
                    applicationBaseUri: uri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: _hostShutdownToken.Token,
                    hostProcess: _application.HostProcess
                );
            }
        }

        private void GetLogsFromFile(string file)
        {
            var arr = new string[0];

            RetryHelper.RetryOperation(() => arr = File.ReadAllLines(Path.Combine(DeploymentParameters.PublishedApplicationRootPath, file)),
                            (ex) => Logger.LogWarning("Could not read log file"),
                            5,
                            200);

            if (arr.Length == 0)
            {
                Logger.LogWarning($"{file} is empty.");
            }

            foreach (var line in arr)
            {
                Logger.LogInformation(line);
            }
        }
    }
}
