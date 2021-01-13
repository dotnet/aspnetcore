// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class HostingEnvironmentTests: FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public HostingEnvironmentTests(IISTestSiteFixture fixture): base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task HostingEnvironmentIsCorrect()
        {
            Assert.Equal(_fixture.DeploymentResult.ContentRoot, await _fixture.Client.GetStringAsync("/ContentRootPath"));
            Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\wwwroot", await _fixture.Client.GetStringAsync("/WebRootPath"));
            Assert.Equal(_fixture.DeploymentResult.ContentRoot, await _fixture.DeploymentResult.HttpClient.GetStringAsync("/CurrentDirectory"));
            Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\", await _fixture.Client.GetStringAsync("/BaseDirectory"));
            Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\", await _fixture.Client.GetStringAsync("/ASPNETCORE_IIS_PHYSICAL_PATH"));
        }
    }
}
