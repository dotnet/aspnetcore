// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class StartupTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public StartupTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly string _dotnetLocation = DotNetCommands.GetDotNetExecutable(RuntimeArchitecture.x64);

        [ConditionalFact]
        public async Task ExpandEnvironmentVariableInWebConfig()
        {
            // Point to dotnet installed in user profile.
            await AssertStarts(
                deploymentParameters =>
                {
                    deploymentParameters.EnvironmentVariables["DotnetPath"] = _dotnetLocation;
                    deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "%DotnetPath%"));
                }
            );
        }

        [ConditionalTheory]
        [InlineData("bogus")]
        [InlineData("c:\\random files\\dotnet.exe")]
        [InlineData(".\\dotnet.exe")]
        public async Task InvalidProcessPath_ExpectServerError(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", path));


            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, @"Invalid or unknown processPath provided in web\.config: processPath = '.+', ErrorCode = '0x80070002'\.");
        }

        [ConditionalFact]
        public async Task StartsWithDotnetLocationWithoutExe()
        {
            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf("."));

            await AssertStarts(
                deploymentParameters =>
                {
                    deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", dotnetLocationWithoutExtension));
                }
            );
        }

        [ConditionalFact]
        public async Task StartsWithDotnetLocationUppercase()
        {
            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf(".")).ToUpperInvariant();
            await AssertStarts(
                deploymentParameters =>
                {
                    deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", dotnetLocationWithoutExtension));
                }
            );
        }

        [ConditionalTheory]
        [InlineData("dotnet")]
        [InlineData("dotnet.EXE")]
        public async Task StartsWithDotnetOnThePath(string path)
        {
            await AssertStarts(
                deploymentParameters =>
                {
                    deploymentParameters.EnvironmentVariables["PATH"] = Path.GetDirectoryName(_dotnetLocation);
                    deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", path));
                }
            );

            // Verify that in this scenario where.exe was invoked only once by shim and request handler uses cached value
            Assert.Equal(1, TestSink.Writes.Count(w => w.Message.Contains("Invoking where.exe to find dotnet.exe")));
        }

        private async Task AssertStarts(Action<IISDeploymentParameters> preDeploy = null)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            preDeploy?.Invoke(deploymentParameters);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22)
                .WithAllApplicationTypes()
                .WithAncmV2InProcess();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HelloWorld(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);
        }

        [ConditionalFact]
        public async Task DetectsOveriddenServer()
        {
            var deploymentResult = await DeployAsync(_fixture.GetBaseDeploymentParameters(_fixture.OverriddenServerWebSite, publish: true));
            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            Assert.Contains(TestSink.Writes, context => context.Message.Contains("Application is running inside IIS process but is not configured to use IIS server"));
        }

        [ConditionalFact]
        public async Task CheckInvalidHostingModelParameter()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("hostingModel", "bogus"));

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, "Unknown hosting model 'bogus'. Please specify either hostingModel=\"inprocess\" or hostingModel=\"outofprocess\" in the web.config file.");
        }
    }
}
