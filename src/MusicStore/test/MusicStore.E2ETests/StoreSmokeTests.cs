// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class StoreSmokeTests : LoggedTest
    {
        [SkipIfEnvironmentVariableNotEnabled("RUN_RUNTIME_STORE_TESTS")]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        public async Task DefaultLocation_Kestrel()
        {
            var serverType = ServerType.Kestrel;
            var testName = $"SmokeTestsUsingStore_{serverType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger(nameof(StoreSmokeTests));
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(), serverType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
                {
                    EnvironmentName = "SocialTesting",
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = Tfm.NetCoreApp50,
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    logger.LogInformation("Published output directory structure:");
                    logger.LogInformation(GetDirectoryStructure(deploymentResult.ContentRoot));

                    var mvcCoreDll = "Microsoft.AspNetCore.Mvc.Core.dll";
                    logger.LogInformation(
                        $"Checking if published output was trimmed by verifying that the dll '{mvcCoreDll}' is not present...");

                    var mvcCoreDllPath = Path.Combine(deploymentResult.ContentRoot, mvcCoreDll);
                    var fileInfo = new FileInfo(mvcCoreDllPath);
                    Assert.False(
                        File.Exists(mvcCoreDllPath),
                        $"The file '{fileInfo.Name}.{fileInfo.Extension}' was not expected to be present in the publish directory");

                    logger.LogInformation($"Published output does not have the dll '{mvcCoreDll}', so the output seems to be trimmed");

                    await SmokeTests.RunTestsAsync(deploymentResult, logger);
                }
            }
        }

        // Get the top level view of the published output directory
        private string GetDirectoryStructure(string publishedOutputDir)
        {
            var directoryStructure = new StringBuilder();
            directoryStructure.AppendLine();
            var dir = new DirectoryInfo(publishedOutputDir);
            foreach (var fileSystemInfo in dir.GetFileSystemInfos())
            {
                var isDirectory = fileSystemInfo as DirectoryInfo;
                if (isDirectory != null)
                {
                    directoryStructure.AppendLine(fileSystemInfo.Name + "/");
                }
                else
                {
                    directoryStructure.AppendLine(fileSystemInfo.Name);
                }
            }
            return directoryStructure.ToString();
        }
    }
}