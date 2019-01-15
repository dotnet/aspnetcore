// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class BackwardsCompatibilityTests : FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public BackwardsCompatibilityTests(IISTestSiteFixture fixture) : base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task CheckBackwardsCompatibilityIsUsed()
        {

            var response = await _fixture.Client.GetAsync("/HelloWorld");
            var handles = _fixture.DeploymentResult.HostProcess.Modules;

            foreach (ProcessModule handle in handles)
            {
                if (handle.ModuleName == "aspnetcorev2.dll")
                {
                    Assert.Equal("12.2.18316.0", handle.FileVersionInfo.FileVersion);
                    return;
                }
            }
            throw new XunitException($"Could not find aspnetcorev2.dll loaded in process {_fixture.DeploymentResult.HostProcess.ProcessName}");
        }
    }
}
