// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using Xunit;

namespace IIS.FunctionalTests.Inprocess
{
    [Collection(PublishedSitesCollection.Name)]
    public class StdOutRedirectionTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;
        private readonly string _logFolderPath;

        public StdOutRedirectionTests(PublishedSitesFixture fixture)
        {
            _logFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _fixture = fixture;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Directory.Exists(_logFolderPath))
            {
                Directory.Delete(_logFolderPath, true);
            }
        }

        [ConditionalFact]
        [SkipIfDebug]
        public async Task FrameworkNotFoundExceptionLogged_Pipe()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            InvalidateRuntimeConfig(deploymentResult);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink,
                "The specified framework 'Microsoft.NETCore.App', version '2.9.9' was not found.");
        }

        [ConditionalFact]
        [SkipIfDebug]
        public async Task FrameworkNotFoundExceptionLogged_File()
        {
            var deploymentParameters =
                _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            InvalidateRuntimeConfig(deploymentResult);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var contents = File.ReadAllText(Helpers.GetExpectedLogName(deploymentResult, _logFolderPath));
            var expectedString = "The specified framework 'Microsoft.NETCore.App', version '2.9.9' was not found.";
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, expectedString);
            Assert.Contains(expectedString, contents);
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [SkipIfDebug]
        public async Task EnableCoreHostTraceLogging_TwoLogFilesCreated()
        {
            var deploymentParameters =
                _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);
            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var fileInDirectory = Directory.GetFiles(_logFolderPath).Single();
            var contents = File.ReadAllText(fileInDirectory);
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, "Invoked hostfxr");
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
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);
            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");

            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, "Invoked hostfxr");
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
                _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);
            deploymentParameters.EnvironmentVariables["COREHOST_TRACE"] = "1";
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path;

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var fileInDirectory = Directory.GetFiles(_logFolderPath).First();
            var contents = File.ReadAllText(fileInDirectory);

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, TestSink, "Invoked hostfxr");
            Assert.Contains("Invoked hostfxr", contents);
        }

        private static void InvalidateRuntimeConfig(IISDeploymentResult deploymentResult)
        {
            var path = Path.Combine(deploymentResult.ContentRoot, "StartupExceptionWebSite.runtimeconfig.json");
            dynamic depsFileContent = JsonConvert.DeserializeObject(File.ReadAllText(path));
            depsFileContent["runtimeOptions"]["framework"]["version"] = "2.9.9";
            var output = JsonConvert.SerializeObject(depsFileContent);
            File.WriteAllText(path, output);
        }
    }
}
