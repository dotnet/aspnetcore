// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class NewShimTests : IISFunctionalTestBase
    {
        public NewShimTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        public async Task CheckNewShimIsUsed()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            var result = await DeployAsync(deploymentParameters);
            var response = await result.HttpClient.GetAsync("/HelloWorld");
            var handles = result.HostProcess.Modules;
            foreach (ProcessModule handle in handles)
            {
                if (handle.ModuleName == "aspnetcorev2_inprocess.dll")
                {
                    Assert.Equal("12.2.18316.0", handle.FileVersionInfo.FileVersion);
                    return;
                }
            }
            throw new XunitException($"Could not find aspnetcorev2_inprocess.dll loaded in process {result.HostProcess.ProcessName}");
        }
    }
}
