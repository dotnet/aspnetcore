// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class StdOutRedirectionTests : LogFileTestBase
    {
        public StdOutRedirectionTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [RequiresNewShim]
        public async Task FrameworkNotFoundExceptionLogged_Pipe()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.ModifyFrameworkVersionInRuntimeConfig(deploymentResult);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult,
                "The framework 'Microsoft.NETCore.App', version '2.9.9' was not found.", Logger);
        }

        [ConditionalFact]
        [RequiresNewShim]
        public async Task FrameworkNotFoundExceptionLogged_File()
        {
            var deploymentParameters =
                Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.ModifyFrameworkVersionInRuntimeConfig(deploymentResult);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, _logFolderPath), Logger);
            var expectedString = "The framework 'Microsoft.NETCore.App', version '2.9.9' was not found.";
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, expectedString, Logger);
            Assert.Contains(expectedString, contents);
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [SkipIfDebug]
        public async Task EnableCoreHostTraceLogging_TwoLogFilesCreated()
        {
            var deploymentParameters =
                Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
            deploymentParameters.TransformArguments((a, _) => $"{a} CheckLargeStdOutWrites");

            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var fileInDirectory = Directory.GetFiles(_logFolderPath).Single();
            var contents = Helpers.ReadAllTextFromFile(fileInDirectory, Logger);
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Invoked hostfxr", Logger);
            Assert.Contains("Invoked hostfxr", contents);
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [SkipIfDebug]
        [InlineData("CheckLargeStdErrWrites")]
        [InlineData("CheckLargeStdOutWrites")]
        [InlineData("CheckOversizedStdErrWrites")]
        [InlineData("CheckOversizedStdOutWrites")]
        public async Task EnableCoreHostTraceLogging_PipeCaptureNativeLogs(string path)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";
            deploymentParameters.TransformArguments((a, _) => $"{a} {path}");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");

            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Invoked hostfxr", Logger);
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [SkipIfDebug]
        [InlineData("CheckLargeStdErrWrites")]
        [InlineData("CheckLargeStdOutWrites")]
        [InlineData("CheckOversizedStdErrWrites")]
        [InlineData("CheckOversizedStdOutWrites")]
        public async Task EnableCoreHostTraceLogging_FileCaptureNativeLogs(string path)
        {
            var deploymentParameters =
                Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";
            deploymentParameters.TransformArguments((a, _) => $"{a} {path}");

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var fileInDirectory = Directory.GetFiles(_logFolderPath).First();
            var contents = Helpers.ReadAllTextFromFile(fileInDirectory, Logger);

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Invoked hostfxr", Logger);
            Assert.Contains("Invoked hostfxr", contents);
        }
    }
}
