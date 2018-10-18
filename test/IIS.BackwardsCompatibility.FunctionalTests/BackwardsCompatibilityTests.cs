// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class BackwardsCompatibilityTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public BackwardsCompatibilityTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task CheckBackwardsCompatibilityIsUsed()
        {
            var iisDeploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            iisDeploymentParameters.PreservePublishedApplicationForDebugging = true;
            var deploymentResult = await DeployAsync(iisDeploymentParameters);

            var arch = iisDeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x64 ? $@"x64\aspnetcorev2.dll" : $@"x86\aspnetcorev2.dll";

            var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(AppContext.BaseDirectory, arch));
            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            var handles = deploymentResult.HostProcess.Modules;
            foreach (ProcessModule handle in handles)
            {
                if (handle.ModuleName == "aspnetcorev2.dll")
                {
                    Assert.Equal("12.2.18283.0", handle.FileVersionInfo.FileVersion);
                    return;
                }
            }
            throw new XunitException($"Could not find aspnetcorev2.dll loaded in process {deploymentResult.HostProcess.ProcessName}");
        }
    }
}
