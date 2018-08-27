// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class LoggingTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;
        private readonly string _logFolderPath;

        public LoggingTests(PublishedSitesFixture fixture)
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

        [ConditionalTheory]
        [InlineData("CheckErrLogFile")]
        [InlineData("CheckLogFile")]
        public async Task CheckStdoutLoggingToFile(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);
            
            await Helpers.AssertStarts(deploymentResult, path);

            StopServer();

            var contents = File.ReadAllText(Helpers.GetExpectedLogName(deploymentResult, _logFolderPath));

            Assert.NotNull(contents);
            Assert.Contains("TEST MESSAGE", contents);
        }

        [ConditionalFact]
        public async Task InvalidFilePathForLogs_ServerStillRuns()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogEnabled", "true"));
            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogFile", Path.Combine("Q:", "std")));

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, "HelloWorld");
        }

        [ConditionalFact]
        public async Task OnlyOneFileCreatedWithProcessStartTime()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);
            await Helpers.AssertStarts(deploymentResult, "CheckLogFile");

            StopServer();

            Assert.Single(Directory.GetFiles(_logFolderPath), Helpers.GetExpectedLogName(deploymentResult, _logFolderPath));
        }

        [ConditionalFact]
        public async Task StartupMessagesAreLoggedIntoDebugLogFile()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.HandlerSettings["debugLevel"] = "file";
            deploymentParameters.HandlerSettings["debugFile"] = "debug.txt";

            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "debug.txt"));
        }

        [ConditionalFact]
        public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFile()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.HandlerSettings["debugLevel"] = "file";

            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFileWhenEnabledWithEnvVar()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG"] = "file";
            // Add empty debugFile handler setting to prevent IIS deployer from overriding debug settings
            deploymentParameters.HandlerSettings["debugFile"] = "";
            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
        }

        [ConditionalTheory]
        [InlineData("CheckErrLogFile")]
        [InlineData("CheckLogFile")]
        public async Task CheckStdoutLoggingToPipe_DoesNotCrashProcess(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, path);

            StopServer();

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                Assert.Contains(TestSink.Writes, context => context.Message.Contains("TEST MESSAGE"));
            }
        }

        [ConditionalTheory]
        [InlineData("CheckErrLogFile")]
        [InlineData("CheckLogFile")]
        public async Task CheckStdoutLoggingToPipeWithFirstWrite(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            var firstWriteString = path + path;

            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_INITIAL_WRITE"] = firstWriteString;

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, path);

            StopServer();

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                // We can't read stdout logs from IIS as they aren't redirected.
                Assert.Contains(TestSink.Writes, context => context.Message.Contains(firstWriteString));
                Assert.Contains(TestSink.Writes, context => context.Message.Contains("TEST MESSAGE"));
            }
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task StartupMessagesLogFileSwitchedWhenLogFilePresentInWebConfig()
        {
            var firstTempFile = Path.GetTempFileName();
            var secondTempFile = Path.GetTempFileName();

            try
            {
                var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
                deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG_FILE"] = firstTempFile;
                deploymentParameters.AddDebugLogToWebConfig(secondTempFile);

                var deploymentResult = await DeployAsync(deploymentParameters);

                var response = await deploymentResult.HttpClient.GetAsync("/");

                StopServer();
                var logContents = File.ReadAllText(firstTempFile);
                Assert.Contains("Switching debug log files to", logContents);

                AssertLogs(secondTempFile);
            }
            finally
            {
                File.Delete(firstTempFile);
                File.Delete(secondTempFile);
            }
        }

        private static void AssertLogs(string logPath)
        {
            using (var stream = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(stream))
            {
                var logContents = streamReader.ReadToEnd();
                Assert.Contains("[aspnetcorev2.dll]", logContents);
                Assert.Contains("[aspnetcorev2_inprocess.dll]", logContents);
                Assert.Contains("Description: IIS ASP.NET Core Module V2. Commit:", logContents);
                Assert.Contains("Description: IIS ASP.NET Core Module V2 Request Handler. Commit:", logContents);
            }
        }
    }
}
