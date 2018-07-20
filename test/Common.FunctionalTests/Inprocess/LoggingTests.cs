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
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class LoggingTests : IISFunctionalTestBase
    {
        [ConditionalTheory]
        [InlineData("CheckErrLogFile")]
        [InlineData("CheckLogFile")]
        public async Task CheckStdoutLoggingToFile(string path)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);

            deploymentParameters.ModifyAspNetCoreSectionInWebConfig("stdoutLogEnabled", "true");

            var pathToLogs = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            deploymentParameters.ModifyAspNetCoreSectionInWebConfig("stdoutLogFile", Path.Combine(pathToLogs, "std"));

            var deploymentResult = await DeployAsync(deploymentParameters);

            try
            {
                await Helpers.AssertStarts(deploymentResult, path);

                StopServer();

                var fileInDirectory = Directory.GetFiles(pathToLogs).Single();

                var contents = File.ReadAllText(fileInDirectory);

                Assert.NotNull(contents);
                Assert.Contains("TEST MESSAGE", contents);
                Assert.DoesNotContain(TestSink.Writes, context => context.Message.Contains("TEST MESSAGE"));
                // TODO we should check that debug logs are restored during graceful shutdown.
                // The IIS Express deployer doesn't support graceful shutdown.
                //Assert.Contains(TestSink.Writes, context => context.Message.Contains("Restoring original stdout: "));
            }
            finally
            {

                RetryHelper.RetryOperation(
                    () => Directory.Delete(pathToLogs, true),
                    e => Logger.LogWarning($"Failed to delete directory : {e.Message}"),
                    retryCount: 3,
                    retryDelayMilliseconds: 100);
            }
        }

        [ConditionalFact]
        public async Task InvalidFilePathForLogs_ServerStillRuns()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);

            deploymentParameters.ModifyAspNetCoreSectionInWebConfig("stdoutLogEnabled", "true");
            deploymentParameters.ModifyAspNetCoreSectionInWebConfig("stdoutLogFile", Path.Combine("Q:", "std"));

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, "HelloWorld");
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        public async Task StartupMessagesAreLoggedIntoDebugLogFile()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
                deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG_FILE"] = tempFile;
                deploymentParameters.AddDebugLogToWebConfig(tempFile);

                var deploymentResult = await DeployAsync(deploymentParameters);

                var response = await deploymentResult.RetryingHttpClient.GetAsync("/");

                StopServer();

                var logContents = File.ReadAllText(tempFile);
                Assert.Contains("[aspnetcorev2.dll]", logContents);
                Assert.Contains("[aspnetcorev2_inprocess.dll]", logContents);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [ConditionalTheory]
        [InlineData("CheckErrLogFile")]
        [InlineData("CheckLogFile")]
        public async Task CheckStdoutLoggingToPipe_DoesNotCrashProcess(string path)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
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
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
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
                var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
                deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG_FILE"] = firstTempFile;
                deploymentParameters.AddDebugLogToWebConfig(secondTempFile);

                var deploymentResult = await DeployAsync(deploymentParameters);

                var response = await deploymentResult.RetryingHttpClient.GetAsync("/");

                StopServer();
                var logContents = File.ReadAllText(firstTempFile);
                Assert.Contains("Switching debug log files to", logContents);

                var secondLogContents = File.ReadAllText(secondTempFile);
                Assert.Contains("[aspnetcorev2.dll]", logContents);
                Assert.Contains("[aspnetcorev2_inprocess.dll]", logContents);
            }
            finally
            {
                File.Delete(firstTempFile);
                File.Delete(secondTempFile);
            }
        }
    }
}
