// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class StartupTests : IISFunctionalTestBase
    {
        private readonly string _dotnetLocation = DotNetMuxer.MuxerPathOrDefault();

        public StartupTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public async Task ExpandEnvironmentVariableInWebConfig()
        {
            // Point to dotnet installed in user profile.
            await AssertStarts(
                deploymentResult => Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", "%DotnetPath%"),
                deploymentParameters => deploymentParameters.EnvironmentVariables["DotnetPath"] = _dotnetLocation);
        }

        [ConditionalTheory]
        [InlineData("bogus")]
        [InlineData("c:\\random files\\dotnet.exe")]
        [InlineData(".\\dotnet.exe")]
        public async Task InvalidProcessPath_ExpectServerError(string path)
        {
            var deploymentParameters = GetBaseDeploymentParameters();

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", path);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [ConditionalFact]
        public async Task StartsWithDotnetLocationWithoutExe()
        {
            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf("."));

            await AssertStarts(
                deploymentResult => Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", dotnetLocationWithoutExtension));
        }

        [ConditionalFact]
        public async Task StartsWithDotnetLocationUppercase()
        {
            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf(".")).ToUpperInvariant();

            await AssertStarts(
                deploymentResult => Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", dotnetLocationWithoutExtension));
        }

        [ConditionalTheory]
        [InlineData("dotnet")]
        [InlineData("dotnet.EXE")]
        public async Task StartsWithDotnetOnThePath(string path)
        {
            await AssertStarts(
                deploymentResult => Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", path),
                deploymentParameters => deploymentParameters.EnvironmentVariables["PATH"] = Path.GetDirectoryName(_dotnetLocation));

            // Verify that in this scenario where.exe was invoked only once by shim and request handler uses cached value
            Assert.Equal(1, TestSink.Writes.Count(w => w.Message.Contains("Invoking where.exe to find dotnet.exe")));
        }

        private async Task AssertStarts(Action<IISDeploymentResult> postDeploy, Action<DeploymentParameters> preDeploy = null)
        {
            var deploymentParameters = GetBaseDeploymentParameters();

            preDeploy?.Invoke(deploymentParameters);

            var deploymentResult = await DeployAsync(deploymentParameters);

            postDeploy?.Invoke(deploymentResult);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("HelloWorld");

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
            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetInProcessTestSitesPath(),
                PublishApplicationBeforeDeployment = true
            };

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("/HelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);
        }

        [ConditionalFact]
        public async Task DetectsOveriddenServer()
        {
            var deploymentResult = await DeployAsync(GetBaseDeploymentParameters("OverriddenServerWebSite"));
            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            Assert.Contains(TestSink.Writes, context => context.Message.Contains("Application is running inside IIS process but is not configured to use IIS server"));
        }

        // Defaults to inprocess specific deployment parameters
        public static DeploymentParameters GetBaseDeploymentParameters(string site = "InProcessWebSite")
        {
            return new DeploymentParameters(Helpers.GetTestWebSitePath(site), DeployerSelector.ServerType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = HostingModel.InProcess,
                PublishApplicationBeforeDeployment = site == "InProcessWebSite",
            };
        }
    }
}
