// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
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
public class StdOutRedirectionTests : IISFunctionalTestBase
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

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
            @"Framework: 'Microsoft.NETCore.App', version '2.9.9' \(x64\)", Logger);
        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
            "To install missing framework, download:", Logger);
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task FrameworkNotFoundExceptionLogged_File()
    {
        var deploymentParameters =
            Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        Helpers.ModifyFrameworkVersionInRuntimeConfig(deploymentResult);

        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.False(response.IsSuccessStatusCode);

        StopServer();

        var contents = Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, LogFolderPath), Logger);
        var missingFrameworkString = "To install missing framework, download:";
        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
            @"Framework: 'Microsoft.NETCore.App', version '2.9.9' \(x64\)", Logger);
        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
            missingFrameworkString, Logger);
        Assert.Contains(@"Framework: 'Microsoft.NETCore.App', version '2.9.9' (x64)", contents);
        Assert.Contains(missingFrameworkString, contents);
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

        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.False(response.IsSuccessStatusCode);

        StopServer();

        var fileInDirectory = Directory.GetFiles(LogFolderPath).Single();
        var contents = Helpers.ReadAllTextFromFile(fileInDirectory, Logger);
        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, "Invoked hostfxr", Logger);
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

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, "Invoked hostfxr", Logger);
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

        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.False(response.IsSuccessStatusCode);

        StopServer();

        var fileInDirectory = Directory.GetFiles(LogFolderPath).First();
        var contents = Helpers.ReadAllTextFromFile(fileInDirectory, Logger);

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, "Invoked hostfxr", Logger);
        Assert.Contains("Invoked hostfxr", contents);
    }
}
