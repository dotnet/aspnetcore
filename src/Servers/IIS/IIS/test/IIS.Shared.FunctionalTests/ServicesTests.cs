// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ApplicationInitializationTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public ApplicationInitializationTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.ApplicationInitialization)]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task ApplicationPreloadStartsApp(HostingModel hostingModel)
        {
            // This test often hits a memory leak in warmup.dll module, it has been reported to IIS team
            using (AppVerifier.Disable(DeployerSelector.ServerType, 0x900))
            {
                var baseDeploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel, publish: true);
                baseDeploymentParameters.TransformArguments(
                    (args, contentRoot) => $"{args} CreateFile \"{Path.Combine(contentRoot, "Started.txt")}\"");
                EnablePreload(baseDeploymentParameters);

                var result = await DeployAsync(baseDeploymentParameters);

                await Helpers.Retry(async () => await File.ReadAllTextAsync(Path.Combine(result.ContentRoot, "Started.txt")), 10, 200);
                StopServer();
                EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.Started(result));
            }
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.ApplicationInitialization)]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task ApplicationInitializationPageIsRequested(HostingModel hostingModel)
        {
            // This test often hits a memory leak in warmup.dll module, it has been reported to IIS team
            using (AppVerifier.Disable(DeployerSelector.ServerType, 0x900))
            {
                var baseDeploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel, publish: true);
                EnablePreload(baseDeploymentParameters);

                baseDeploymentParameters.ServerConfigActionList.Add(
                    (config, _) => {
                        config
                            .RequiredElement("system.webServer")
                            .GetOrAdd("applicationInitialization")
                            .GetOrAdd("add", "initializationPage", "/CreateFile");
                    });

                var result = await DeployAsync(baseDeploymentParameters);

                await Helpers.Retry(async () => await File.ReadAllTextAsync(Path.Combine(result.ContentRoot, "Started.txt")), 10, 200);
                StopServer();
                EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.Started(result));
            }
        }

        private static void EnablePreload(IISDeploymentParameters baseDeploymentParameters)
        {
            baseDeploymentParameters.EnsureSection("applicationInitialization", "system.webServer");
            baseDeploymentParameters.ServerConfigActionList.Add(
                (config, _) => {

                    config
                        .RequiredElement("system.applicationHost")
                        .RequiredElement("sites")
                        .RequiredElement("site")
                        .RequiredElement("application")
                        .SetAttributeValue("preloadEnabled", true);
                });

            baseDeploymentParameters.EnableModule("ApplicationInitializationModule", "%IIS_BIN%\\warmup.dll");
        }
    }
}
