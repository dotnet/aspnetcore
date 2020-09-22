// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class NewShimTests : FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public NewShimTests(IISTestSiteFixture fixture) : base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task CheckNewShimIsUsed()
        {
            var response = await _fixture.Client.GetAsync("/HelloWorld");
            var handles = _fixture.DeploymentResult.HostProcess.Modules;
            foreach (ProcessModule handle in handles)
            {
                if (handle.ModuleName == "aspnetcorev2_inprocess.dll")
                {
                    Assert.Equal("12.2.18316.0", handle.FileVersionInfo.FileVersion);
                    return;
                }
            }
            throw new XunitException($"Could not find aspnetcorev2_inprocess.dll loaded in process {_fixture.DeploymentResult.HostProcess.ProcessName}");
        }
    }
}
