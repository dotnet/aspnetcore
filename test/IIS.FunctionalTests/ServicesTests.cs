// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ServiceProcess;
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
        public async Task ApplicationInitializationInitializedInProc(HostingModel hostingModel)
        {
            var baseDeploymentParameters = _fixture.GetBaseDeploymentParameters(hostingModel);
            EnableAppInitialization(baseDeploymentParameters);

            var result = await DeployAsync(baseDeploymentParameters);

            // Allow IIS a bit of time to complete starting before we start checking
            await Task.Delay(100);
            // There is always a race between which Init request arrives first
            // retry couple times to see if we ever get the one comming from ApplicationInitialization module
            await result.HttpClient.RetryRequestAsync("/ApplicationInitialization", async message => await message.Content.ReadAsStringAsync() == "True");

            StopServer();
            EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.Started(result));
        }

        private static void EnableAppInitialization(IISDeploymentParameters baseDeploymentParameters)
        {
            baseDeploymentParameters.ServerConfigActionList.Add(
                (config, _) => {
                    config
                        .RequiredElement("configSections")
                        .GetOrAdd("sectionGroup", "name", "system.webServer")
                        .GetOrAdd("section", "name", "applicationInitialization")
                        .SetAttributeValue("overrideModeDefault", "Allow");

                    config
                        .RequiredElement("system.applicationHost")
                        .RequiredElement("sites")
                        .RequiredElement("site")
                        .RequiredElement("application")
                        .SetAttributeValue("preloadEnabled", true);

                    config
                        .RequiredElement("system.webServer")
                        .GetOrAdd("applicationInitialization")
                        .GetOrAdd("add", "initializationPage", "/ApplicationInitialization?IISInit=true");
                });

            baseDeploymentParameters.EnableModule("ApplicationInitializationModule", "%IIS_BIN%\\warmup.dll");
        }
    }
}
