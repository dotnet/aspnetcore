// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
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
        public async Task HostingEnvironmentIsCorrect()
        {
            Assert.Equal(
                $"ContentRootPath {_fixture.DeploymentResult.ContentRoot}" + Environment.NewLine +
                $"WebRootPath {_fixture.DeploymentResult.ContentRoot}\\wwwroot" + Environment.NewLine +
                $"CurrentDirectory {_fixture.DeploymentResult.ContentRoot}" + Environment.NewLine +
                $"BaseDirectory {_fixture.DeploymentResult.ContentRoot}\\",
                await _fixture.Client.GetStringAsync("/HostingEnvironment"));

            Assert.Equal(Path.GetDirectoryName(_fixture.DeploymentResult.HostProcess.MainModule.FileName),
                await _fixture.DeploymentResult.HttpClient.GetStringAsync("/DllDirectory"));
        }
    }
}
