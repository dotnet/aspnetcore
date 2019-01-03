// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class LoggingTests : LogFileTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public LoggingTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp30)
                .WithAllApplicationTypes()
                .WithAncmVersions(AncmVersion.AspNetCoreModuleV2)
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task CheckStdoutLoggingToFile(TestVariant variant)
        {
            await CheckStdoutToFile(variant, "ConsoleWrite");
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task CheckStdoutErrLoggingToFile(TestVariant variant)
        {
            await CheckStdoutToFile(variant, "ConsoleErrorWrite");
        }

        private async Task CheckStdoutToFile(TestVariant variant, string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, path);

            StopServer();

            var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, _logFolderPath), Logger);

            Assert.Contains("TEST MESSAGE", contents);
        }

        // Move to separate file
        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task InvalidFilePathForLogs_ServerStillRuns(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);

            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogEnabled", "true"));
            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogFile", Path.Combine("Q:", "std")));

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, "HelloWorld");

            StopServer();
            if (variant.HostingModel == HostingModel.InProcess)
            {
                EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Could not start stdout redirection in (.*)aspnetcorev2.dll. Exception message: HRESULT 0x80070003");
                EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Could not stop stdout redirection in (.*)aspnetcorev2.dll. Exception message: HRESULT 0x80070002");
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task StartupMessagesAreLoggedIntoDebugLogFile(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.HandlerSettings["debugLevel"] = "file";
            deploymentParameters.HandlerSettings["debugFile"] = "debug.txt";

            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "debug.txt"));
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFile(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.HandlerSettings["debugLevel"] = "file";

            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [MemberData(nameof(TestVariants))]
        public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFileWhenEnabledWithEnvVar(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG"] = "file";
            // Add empty debugFile handler setting to prevent IIS deployer from overriding debug settings
            deploymentParameters.HandlerSettings["debugFile"] = "";
            var deploymentResult = await DeployAsync(deploymentParameters);

            await deploymentResult.HttpClient.GetAsync("/");

            AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [MemberData(nameof(TestVariants))]
        public async Task StartupMessagesLogFileSwitchedWhenLogFilePresentInWebConfig(TestVariant variant)
        {
            var firstTempFile = Path.GetTempFileName();
            var secondTempFile = Path.GetTempFileName();

            try
            {
                var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
                deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG_FILE"] = firstTempFile;
                deploymentParameters.AddDebugLogToWebConfig(secondTempFile);

                var deploymentResult = await DeployAsync(deploymentParameters);

                var response = await deploymentResult.HttpClient.GetAsync("/");

                StopServer();
                var logContents = Helpers.ReadAllTextFromFile(firstTempFile, Logger);

                Assert.Contains("Switching debug log files to", logContents);

                AssertLogs(secondTempFile);
            }
            finally
            {
                File.Delete(firstTempFile);
                File.Delete(secondTempFile);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]

        public async Task DebugLogsAreWrittenToEventLog(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.HandlerSettings["debugLevel"] = "file,eventlog";
            var deploymentResult = await StartAsync(deploymentParameters);
            StopServer();
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, @"\[aspnetcorev2.dll\] Initializing logs for .*?Description: IIS ASP.NET Core Module V2");
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task CheckUTF8File(TestVariant variant)
        {
            var path = "CheckConsoleFunctions";

            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.InProcessTestSite, variant.HostingModel, publish: true);
            deploymentParameters.TransformArguments((a, _) => $"{a} {path}"); // For standalone this will need to remove space

            var logFolderPath = _logFolderPath + "\\彡⾔";
            deploymentParameters.EnableLogging(logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync(path);

            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, logFolderPath), Logger);
            Assert.Contains("彡⾔", contents);

            if (variant.HostingModel == HostingModel.InProcess)
            {
                EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessThreadExitStdOut(deploymentResult, "12", "(.*)彡⾔(.*)"));
            }
            else
            {
                EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.OutOfProcessFailedToStart(deploymentResult));
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task OnlyOneFileCreatedWithProcessStartTime(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);

            deploymentParameters.EnableLogging(_logFolderPath);

            var deploymentResult = await DeployAsync(deploymentParameters);
            await Helpers.AssertStarts(deploymentResult, "ConsoleWrite");

            StopServer();

            Assert.Single(Directory.GetFiles(_logFolderPath), Helpers.GetExpectedLogName(deploymentResult, _logFolderPath));
        }

        private static string ReadLogs(string logPath)
        {
            using (var stream = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        private static void AssertLogs(string logPath)
        {
            var logContents = ReadLogs(logPath);
            Assert.Contains("[aspnetcorev2.dll]", logContents);
            Assert.True(logContents.Contains("[aspnetcorev2_inprocess.dll]") || logContents.Contains("[aspnetcorev2_outofprocess.dll]"));
            Assert.Contains("Description: IIS ASP.NET Core Module V2. Commit:", logContents);
            Assert.Contains("Description: IIS ASP.NET Core Module V2 Request Handler. Commit:", logContents);
        }
    }
}
