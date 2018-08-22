// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        [InlineData("bogus", "", @"Executable was not found at '.*?\\bogus.exe")]
        [InlineData("c:\\random files\\dotnet.exe", "something.dll", @"Could not find dotnet.exe at '.*?\\dotnet.exe'")]
        [InlineData(".\\dotnet.exe", "something.dll", @"Could not find dotnet.exe at '.*?\\.\\dotnet.exe'")]
        [InlineData("dotnet.exe", "", @"Application arguments are empty.")]
        [InlineData("dotnet.zip", "", @"Process path 'dotnet.zip' doesn't have '.exe' extension.")]
        public async Task InvalidProcessPath_ExpectServerError(string path, string arguments, string subError)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", path));
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("arguments", arguments));

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, $@"Application '{Regex.Escape(deploymentResult.ContentRoot)}\\' wasn't able to start. {subError}");
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
        public async Task StartsWithPortableAndBootstraperExe()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.InProcessTestSite, publish: true);
            // rest publisher as it doesn't support additional parameters
            deploymentParameters.ApplicationPublisher = null;
            // ReferenceTestTasks is workaround for https://github.com/dotnet/sdk/issues/2482
            deploymentParameters.AdditionalPublishParameters = "-p:RuntimeIdentifier=win7-x64 -p:UseAppHost=true -p:SelfContained=false -p:ReferenceTestTasks=false";
            deploymentParameters.RestoreOnPublish = true;
            var deploymentResult = await DeployAsync(deploymentParameters);

            Assert.True(File.Exists(Path.Combine(deploymentResult.ContentRoot, "InProcessWebSite.exe")));
            Assert.False(File.Exists(Path.Combine(deploymentResult.ContentRoot, "hostfxr.dll")));
            Assert.Contains("InProcessWebSite.exe", File.ReadAllText(Path.Combine(deploymentResult.ContentRoot, "web.config")));

            await deploymentResult.AssertStarts();
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
