// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(PublishedSitesCollection.Name)]

    public class EnvironmentVariableTests: IISFunctionalTestBase
    {
        public EnvironmentVariableTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalTheory]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task GetLongEnvironmentVariable(HostingModel hostingModel)
        {
            var expectedValue = "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative";


            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"] = expectedValue;

            Assert.Equal(
                expectedValue,
                await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public Task AuthHeaderEnvironmentVariableRemoved_InProcess() => AuthHeaderEnvironmentVariableRemoved(HostingModel.InProcess);

        [ConditionalFact]
        public Task AuthHeaderEnvironmentVariableRemoved_OutOfProcess() => AuthHeaderEnvironmentVariableRemoved(HostingModel.OutOfProcess);

        private async Task AuthHeaderEnvironmentVariableRemoved(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_IIS_HTTPAUTH"] = "shouldberemoved";

            Assert.DoesNotContain("shouldberemoved", await GetStringAsync(deploymentParameters,"/GetEnvironmentVariable?name=ASPNETCORE_IIS_HTTPAUTH"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigOverridesGlobalEnvironmentVariables_InProcess() => WebConfigOverridesGlobalEnvironmentVariables(HostingModel.InProcess);

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigOverridesGlobalEnvironmentVariables_OutOfProcess() => WebConfigOverridesGlobalEnvironmentVariables(HostingModel.OutOfProcess);

        private async Task WebConfigOverridesGlobalEnvironmentVariables(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";
            Assert.Equal("Production", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_ENVIRONMENT"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigAppendsHostingStartup_InProcess() => WebConfigAppendsHostingStartup(HostingModel.InProcess);

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigAppendsHostingStartup_OutOfProcess() => WebConfigAppendsHostingStartup(HostingModel.OutOfProcess);

        private async Task WebConfigAppendsHostingStartup(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.EnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm1";
            if (hostingModel == HostingModel.InProcess)
            {
                Assert.Equal("Asm1", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
            }
            else
            {
                Assert.Equal("Asm1;Microsoft.AspNetCore.Server.IISIntegration", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
            }
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigOverridesHostingStartup_InProcess() => WebConfigOverridesHostingStartup(HostingModel.InProcess);

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigOverridesHostingStartup_OutOfProcess() => WebConfigOverridesHostingStartup(HostingModel.OutOfProcess);

        private async Task WebConfigOverridesHostingStartup(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.EnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm1";
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm2";
            Assert.Equal("Asm2", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigExpandsVariables_InProcess() => WebConfigExpandsVariables(HostingModel.InProcess);

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public Task WebConfigExpandsVariables_OutOfProcess() => WebConfigExpandsVariables(HostingModel.OutOfProcess);

        private async Task WebConfigExpandsVariables(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            deploymentParameters.EnvironmentVariables["TestVariable"] = "World";
            deploymentParameters.WebConfigBasedEnvironmentVariables["OtherVariable"] = "%TestVariable%;Hello";
            Assert.Equal("World;Hello", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=OtherVariable"));
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [RequiresNewHandler]
        [RequiresNewShim]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task PreferEnvironmentVariablesOverWebConfigWhenConfigured(HostingModel hostingModel)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);

            var environment = "Development";
            deploymentParameters.EnvironmentVariables["ANCM_PREFER_ENVIRONMENT_VARIABLES"] = "true";
            deploymentParameters.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
            deploymentParameters.WebConfigBasedEnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Debug");
            Assert.Equal(environment, await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_ENVIRONMENT"));
        }
    }
}
