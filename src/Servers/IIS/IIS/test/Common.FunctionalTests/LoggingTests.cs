// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class LoggingTests : IISFunctionalTestBase
{
    public LoggingTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    public static TestMatrix TestVariants
        => TestMatrix.ForServers(DeployerSelector.ServerType)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithAllHostingModels();

    public static TestMatrix InprocessTestVariants
        => TestMatrix.ForServers(DeployerSelector.ServerType)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithHostingModels(HostingModel.InProcess);

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
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        await Helpers.AssertStarts(deploymentResult, path);

        StopServer();

        var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, LogFolderPath), Logger);

        Assert.Contains("TEST MESSAGE", contents);
        Assert.DoesNotContain("\r\n\r\n", contents);
        Assert.Contains("\r\n", contents);
    }

    // Move to separate file
    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    public async Task InvalidFilePathForLogs_ServerStillRuns(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);

        deploymentParameters.WebConfigActionList.Add(
            WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogEnabled", "true"));
        deploymentParameters.WebConfigActionList.Add(
            WebConfigHelpers.AddOrModifyAspNetCoreSection("stdoutLogFile", Path.Combine("Q:", "std")));

        var deploymentResult = await DeployAsync(deploymentParameters);

        await Helpers.AssertStarts(deploymentResult, "HelloWorld");

        StopServer();
        if (variant.HostingModel == HostingModel.InProcess)
        {
            // Error is getting logged twice, from shim and handler
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.CouldNotStartStdoutFileRedirection("Q:\\std", deploymentResult), Logger, allowMultiple: true);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(InprocessTestVariants))]
    [RequiresNewShim]
    public async Task StartupMessagesAreLoggedIntoDebugLogFile(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.HandlerSettings["debugLevel"] = "file";
        deploymentParameters.HandlerSettings["debugFile"] = "subdirectory\\debug.txt";

        var deploymentResult = await DeployAsync(deploymentParameters);

        await deploymentResult.HttpClient.GetAsync("/");

        AssertLogs(Path.Combine(deploymentResult.ContentRoot, "subdirectory", "debug.txt"));
    }

    [ConditionalTheory]
    [MemberData(nameof(InprocessTestVariants))]
    public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFile(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.HandlerSettings["debugLevel"] = "file";

        var deploymentResult = await DeployAsync(deploymentParameters);

        await deploymentResult.HttpClient.GetAsync("/");

        AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [MemberData(nameof(InprocessTestVariants))]
    public async Task StartupMessagesAreLoggedIntoDefaultDebugLogFileWhenEnabledWithEnvVar(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG"] = "file";
        // Add empty debugFile handler setting to prevent IIS deployer from overriding debug settings
        deploymentParameters.HandlerSettings["debugFile"] = "";
        var deploymentResult = await DeployAsync(deploymentParameters);

        await deploymentResult.HttpClient.GetAsync("/");

        AssertLogs(Path.Combine(deploymentResult.ContentRoot, "aspnetcore-debug.log"));
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [MemberData(nameof(InprocessTestVariants))]
    public async Task StartupMessagesLogFileSwitchedWhenLogFilePresentInWebConfig(TestVariant variant)
    {
        var firstTempFile = Path.GetTempFileName();
        var secondTempFile = Path.GetTempFileName();

        try
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
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
    [MemberData(nameof(InprocessTestVariants))]
    public async Task DebugLogsAreWrittenToEventLog(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.HandlerSettings["debugLevel"] = "file,eventlog";
        var deploymentResult = await StartAsync(deploymentParameters);
        StopServer();
        EventLogHelpers.VerifyEventLogEvent(deploymentResult, @"\[aspnetcorev2.dll\] Initializing logs for .*?Description: IIS ASP.NET Core Module V2", Logger);
    }

    [ConditionalTheory]
    [MemberData(nameof(InprocessTestVariants))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/38957")]
    public async Task CheckUTF8File(TestVariant variant)
    {
        var path = "CheckConsoleFunctions";

        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite, variant.HostingModel);
        deploymentParameters.TransformArguments((a, _) => $"{a} {path}"); // For standalone this will need to remove space

        var logFolderPath = LogFolderPath + "\\彡⾔";
        deploymentParameters.EnableLogging(logFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync(path);

        Assert.False(response.IsSuccessStatusCode);

        StopServer();

        var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, logFolderPath), Logger);
        Assert.Contains("彡⾔", contents);
        EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessThreadExitStdOut(deploymentResult, "12", "(.*)彡⾔(.*)"), Logger);
    }

    [ConditionalTheory]
    [MemberData(nameof(InprocessTestVariants))]
    public async Task OnlyOneFileCreatedWithProcessStartTime(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);

        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);
        await Helpers.AssertStarts(deploymentResult, "ConsoleWrite");

        StopServer();

        Assert.Single(Directory.GetFiles(LogFolderPath), Helpers.GetExpectedLogName(deploymentResult, LogFolderPath));
    }

    [ConditionalFact]
    public async Task CaptureLogsForOutOfProcessWhenProcessFailsToStart()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.TransformArguments((a, _) => $"{a} ConsoleWriteSingle");
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("Test");

        StopServer();

        EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.OutOfProcessFailedToStart(deploymentResult, "Wow!"), Logger);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task DisableRedirectionNoLogs()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.HandlerSettings["enableOutOfProcessConsoleRedirection"] = "false";
        deploymentParameters.TransformArguments((a, _) => $"{a} ConsoleWriteSingle");
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("Test");

        StopServer();

        EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.OutOfProcessFailedToStart(deploymentResult, ""), Logger);
    }

    [ConditionalFact]
    public async Task CaptureLogsForOutOfProcessWhenProcessFailsToStart30KbMax()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.TransformArguments((a, _) => $"{a} ConsoleWrite30Kb");
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("Test");

        StopServer();

        EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.OutOfProcessFailedToStart(deploymentResult, new string('a', 30000)), Logger);
    }

    [ConditionalTheory]
    [InlineData("ConsoleErrorWriteStartServer")]
    [InlineData("ConsoleWriteStartServer")]
    public async Task CheckStdoutLoggingToPipeWithFirstWrite(string path)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        var firstWriteString = "TEST MESSAGE";

        deploymentParameters.TransformArguments((a, _) => $"{a} {path}");

        var deploymentResult = await DeployAsync(deploymentParameters);

        await Helpers.AssertStarts(deploymentResult);

        StopServer();

        if (deploymentParameters.ServerType == ServerType.IISExpress)
        {
            // We can't read stdout logs from IIS as they aren't redirected.
            Assert.Contains(TestSink.Writes, context => context.Message.Contains(firstWriteString));
        }
    }

    [ConditionalFact]
    public async Task LogsContainTimestampAndPID()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        var deploymentResult = await DeployAsync(deploymentParameters);

        await Helpers.AssertStarts(deploymentResult);

        StopServer();

        var aspnetcorev2Log = TestSink.Writes.First(w => w.Message.Contains("Description: IIS ASP.NET Core Module V2. Commit:"));
        var aspnetcoreHandlerLog = TestSink.Writes.First(w => w.Message.Contains("Description: IIS ASP.NET Core Module V2 Request Handler. Commit:"));

        var processIdPattern = new Regex("Process Id: (\\d+)\\.", RegexOptions.Singleline);
        var processIdMatch = processIdPattern.Match(aspnetcorev2Log.Message);
        Assert.True(processIdMatch.Success, $"'{processIdPattern}' did not match '{aspnetcorev2Log}'");
        var processId = int.Parse(processIdMatch.Groups[1].Value, CultureInfo.InvariantCulture);

        if (DeployerSelector.HasNewShim)
        {
            AssertTimestampAndPIDPrefix(processId, aspnetcorev2Log.Message);
        }
        if (DeployerSelector.HasNewHandler)
        {
            AssertTimestampAndPIDPrefix(processId, aspnetcoreHandlerLog.Message);
        }
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

    private static void AssertTimestampAndPIDPrefix(int processId, string log)
    {
        var prefixPattern = new Regex(@"\[(.{24}), PID: (\d+)\]", RegexOptions.Singleline);
        var prefixMatch = prefixPattern.Match(log);
        Assert.True(prefixMatch.Success, $"'{prefixPattern}' did not match '{log}'");

        var time = DateTime.Parse(prefixMatch.Groups[1].Value, CultureInfo.InvariantCulture).ToUniversalTime();
        var prefixProcessId = int.Parse(prefixMatch.Groups[2].Value, CultureInfo.InvariantCulture);

        Assert.Equal(processId, prefixProcessId);

        // exact time check isn't reasonable so let's just verify it was somewhat recent
        // log shouldn't happen in the future
        Assert.True(DateTime.UtcNow > time);
        // log shouldn't have been more than two hours ago
        Assert.True(DateTime.UtcNow.AddHours(-2) < time);
    }
}
