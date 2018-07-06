// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [SkipIfIISCannotRun]
    public class IISTests : FunctionalTestsBase
    {
        [ConditionalFact]
        public Task HelloWorld_IIS_CoreClr_X64_Standalone()
        {
            return HelloWorld(RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        public Task HelloWorld_IIS_CoreClr_X64_Portable()
        {
            return HelloWorld(RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        private async Task HelloWorld(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters();
            deploymentParameters.ServerType = ServerType.IIS;
            deploymentParameters.ApplicationType = applicationType;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);
        }
    }
}
