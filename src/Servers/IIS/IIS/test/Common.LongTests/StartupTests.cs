// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Win32;

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

// Contains all tests related to Startup, requiring starting ANCM/IIS every time.
[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;" + "Windows.Amd64.VS2022.Pre;")]
public class StartupTests : IISFunctionalTestBase
{
    public StartupTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    private readonly string _dotnetLocation = DotNetCommands.GetDotNetExecutable(RuntimeArchitecture.x64);

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task ExpandEnvironmentVariableInWebConfig()
    {
        // Point to dotnet installed in user profile.
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.EnvironmentVariables["DotnetPath"] = _dotnetLocation;
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "%DotnetPath%"));
        await StartAsync(deploymentParameters);
    }

    [ConditionalTheory]
    [InlineData("bogus", "", @"Executable was not found at '.*?\\bogus.exe")]
    [InlineData("c:\\random files\\dotnet.exe", "something.dll", @"Could not find dotnet.exe at '.*?\\dotnet.exe'")]
    [InlineData(".\\dotnet.exe", "something.dll", @"Could not find dotnet.exe at '.*?\\.\\dotnet.exe'")]
    [InlineData("dotnet.exe", "", @"Application arguments are empty.")]
    [InlineData("dotnet.zip", "", @"Process path 'dotnet.zip' doesn't have '.exe' extension.")]
    public async Task InvalidProcessPath_ExpectServerError(string path, string arguments, string subError)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", path));
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("arguments", arguments));

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.UnableToStart(deploymentResult, subError), Logger);
        if (DeployerSelector.HasNewShim)
        {
            Assert.Contains("500.0", await response.Content.ReadAsStringAsync());
        }
        else
        {
            Assert.Contains("500.0", await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task StartsWithDotnetLocationWithoutExe()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf(".", StringComparison.Ordinal));
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", dotnetLocationWithoutExtension));

        await StartAsync(deploymentParameters);
    }

    [ConditionalFact]
    public async Task StartsWithDotnetLocationUppercase()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf(".", StringComparison.Ordinal)).ToUpperInvariant();
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", dotnetLocationWithoutExtension));

        await StartAsync(deploymentParameters);
    }

    [ConditionalTheory]
    [InlineData("dotnet")]
    [InlineData("dotnet.EXE")]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task StartsWithDotnetOnThePath(string path)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        deploymentParameters.EnvironmentVariables["PATH"] = Path.GetDirectoryName(_dotnetLocation);
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", path));

        var deploymentResult = await DeployAsync(deploymentParameters);
        await deploymentResult.AssertStarts();

        StopServer();
        // Verify that in this scenario where.exe was invoked only once by shim and request handler uses cached value
        Assert.Equal(1, TestSink.Writes.Count(w => w.Message.Contains("Invoking where.exe to find dotnet.exe")));
    }

    [ConditionalFact]
    [SkipIfNotAdmin]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task StartsWithDotnetInstallLocation()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.RuntimeArchitecture = RuntimeArchitecture.x64;

        // IIS doesn't allow empty PATH
        deploymentParameters.EnvironmentVariables["PATH"] = ".";
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "dotnet"));

        // Key is always in 32bit view
        using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        {
            var installDir = DotNetCommands.GetDotNetInstallDir(RuntimeArchitecture.x64);
            using (new TestRegistryKey(
                localMachine,
                "SOFTWARE\\dotnet\\Setup\\InstalledVersions\\" + RuntimeArchitecture.x64,
                "InstallLocation",
                installDir))
            {
                var deploymentResult = await DeployAsync(deploymentParameters);
                await deploymentResult.AssertStarts();
                StopServer();
                // Verify that in this scenario dotnet.exe was found using InstallLocation lookup
                // I would've liked to make a copy of dotnet directory in this test and use it for verification
                // but dotnet roots are usually very large on dev machines so this test would take disproportionally long time and disk space
                Assert.Equal(1, TestSink.Writes.Count(w => w.Message.Contains($"Found dotnet.exe in InstallLocation at '{installDir}\\dotnet.exe'")));
            }
        }
    }

    [ConditionalFact]
    [SkipIfNotAdmin]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task DoesNotStartIfDisabled()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        using (new TestRegistryKey(
            Registry.LocalMachine,
            "SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module V2\\Parameters",
            "DisableANCM",
            1))
        {
            var deploymentResult = await DeployAsync(deploymentParameters);
            // Disabling ANCM produces no log files
            deploymentResult.AllowNoLogs();

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");

            Assert.False(response.IsSuccessStatusCode);

            StopServer();

            await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, "AspNetCore Module is disabled", Logger);
        }
    }

    public static TestMatrix TestVariants
        => TestMatrix.ForServers(DeployerSelector.ServerType)
            .WithTfms(Tfm.Default)
            .WithAllApplicationTypes()
            .WithAncmV2InProcess();

    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    public async Task HelloWorld(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        await StartAsync(deploymentParameters);
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task StartsWithPortableAndBootstraperExe()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformPath((path, root) => "InProcessWebSite.exe");
        deploymentParameters.TransformArguments((arguments, root) => "");

        // We need the right dotnet on the path in IIS
        deploymentParameters.EnvironmentVariables["PATH"] = Path.GetDirectoryName(DotNetCommands.GetDotNetExecutable(deploymentParameters.RuntimeArchitecture));

        var deploymentResult = await DeployAsync(deploymentParameters);

        Assert.True(File.Exists(Path.Combine(deploymentResult.ContentRoot, "InProcessWebSite.exe")));
        Assert.False(File.Exists(Path.Combine(deploymentResult.ContentRoot, "hostfxr.dll")));
        Assert.Contains("InProcessWebSite.exe", Helpers.ReadAllTextFromFile(Path.Combine(deploymentResult.ContentRoot, "web.config"), Logger));

        await deploymentResult.AssertStarts();
    }

    [ConditionalFact]
    public async Task DetectsOverriddenServer()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} OverriddenServer");

        var deploymentResult = await DeployAsync(deploymentParameters);
        var response = await deploymentResult.HttpClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        await EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.InProcessFailedToStart(deploymentResult, "CLR worker thread exited prematurely"),
            EventLogHelpers.InProcessThreadException(deploymentResult, ".*?Application is running inside IIS process but is not configured to use IIS server"));
    }

    [ConditionalFact]
    public async Task LogsStartupExceptionExitError()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} Throw");

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        await EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.InProcessFailedToStart(deploymentResult, "CLR worker thread exited prematurely"),
            EventLogHelpers.InProcessThreadException(deploymentResult, ", exception code = '0xe0434352'"));
    }

    [ConditionalFact]
    public async Task LogsUnexpectedThreadExitError()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} EarlyReturn");
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        await EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.InProcessFailedToStart(deploymentResult, "CLR worker thread exited prematurely"),
            EventLogHelpers.InProcessThreadExit(deploymentResult, "12"));
    }

    [ConditionalFact]
    public async Task RemoveHostfxrFromApp_InProcessHostfxrAPIAbsent()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        var deploymentResult = await DeployAsync(deploymentParameters);

        File.Copy(
            Path.Combine(deploymentResult.ContentRoot, "aspnetcorev2_inprocess.dll"),
            Path.Combine(deploymentResult.ContentRoot, "hostfxr.dll"),
            true);

        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.32");
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);
        }

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessHostfxrInvalid(deploymentResult), Logger);
    }

    [ConditionalFact]
    public async Task PublishWithWrongBitness()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

        if (deploymentParameters.ServerType == ServerType.IISExpress)
        {
            return;
        }

        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        deploymentParameters.AddServerConfigAction(element =>
        {
            element.RequiredElement("system.applicationHost").RequiredElement("applicationPools").RequiredElement("add").SetAttributeValue("enable32BitAppOnWin64", "true");
        });

        // Change ANCM dll to 32 bit
        deploymentParameters.AddServerConfigAction(
                        element =>
                        {
                            var ancmElement = element
                                .RequiredElement("system.webServer")
                                .RequiredElement("globalModules")
                                .Elements("add")
                                .FirstOrDefault(e => e.Attribute("name").Value == "AspNetCoreModuleV2");

                            ancmElement.SetAttributeValue("image", ancmElement.Attribute("image").Value.Replace("x64", "x86"));
                        });
        var deploymentResult = await DeployAsync(deploymentParameters);

        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.32");
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.0");
        }
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task RemoveHostfxrFromApp_InProcessHostfxrLoadFailure()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        var deploymentResult = await DeployAsync(deploymentParameters);

        // We don't distinguish between load failure types so making dll empty should be enough
        File.WriteAllText(Path.Combine(deploymentResult.ContentRoot, "hostfxr.dll"), "");

        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.32");
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);
        }

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessHostfxrUnableToLoad(deploymentResult), Logger);
    }

    [ConditionalFact]
    public async Task TargetDifferenceSharedFramework_FailedToFindNativeDependencies()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        var deploymentResult = await DeployAsync(deploymentParameters);

        Helpers.ModifyFrameworkVersionInRuntimeConfig(deploymentResult);
        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.31");
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);
        }

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessFailedToFindNativeDependencies(deploymentResult), Logger);
    }

    [ConditionalFact]
    [RequiresNewShim]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52889")]
    public async Task WrongApplicationPath_FailedToRun()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "TRUE";
        var deploymentResult = await DeployAsync(deploymentParameters);

        deploymentResult.ModifyWebConfig(element => element
            .Descendants("system.webServer")
            .Single()
            .GetOrAdd("aspNetCore")
            .SetAttributeValue("arguments", "not-exist.dll"));

        await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.31", "Provided application path does not exist, or isn't a .dll or .exe.");

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessFailedToFindApplication(), Logger);
    }

    [ConditionalFact]
    public async Task SingleExecutable_FailedToFindNativeDependencies()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        var deploymentResult = await DeployAsync(deploymentParameters);

        File.Delete(Path.Combine(deploymentResult.ContentRoot, "InProcessWebSite.dll"));
        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.38");
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);
        }
    }

    [ConditionalFact]
    public async Task TargedDifferenceSharedFramework_FailedToFindNativeDependenciesErrorInResponse()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "TRUE";
        var deploymentResult = await DeployAsync(deploymentParameters);

        Helpers.ModifyFrameworkVersionInRuntimeConfig(deploymentResult);
        if (DeployerSelector.HasNewShim)
        {
            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("500.31", responseContent);
            Assert.Contains("Framework: 'Microsoft.NETCore.App', version '2.9.9'", responseContent);
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);
        }
    }

    [ConditionalFact]
    public async Task RemoveInProcessReference_FailedToFindRequestHandler()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        var deploymentResult = await DeployAsync(deploymentParameters);

        File.Delete(Path.Combine(deploymentResult.ContentRoot, "aspnetcorev2_inprocess.dll"));

        if (DeployerSelector.HasNewShim)
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.33");

            await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessFailedToFindRequestHandler(deploymentResult), Logger);
        }
        else
        {
            await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);

            await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessFailedToFindRequestHandler(deploymentResult), Logger);
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task StartupTimeoutIsApplied()
    {
        // From what we can tell, this failure is due to ungraceful shutdown.
        // The error could be the same as https://github.com/dotnet/core-setup/issues/4646
        // But can't be certain without another repro.
        using (AppVerifier.Disable(DeployerSelector.ServerType, 0x300))
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
            deploymentParameters.TransformArguments((a, _) => $"{a} Hang");
            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("startupTimeLimit", "1"));

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            // Startup timeout now recycles process.
            deploymentResult.AssertWorkerProcessStop();

            await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
                EventLogHelpers.InProcessFailedToStart(deploymentResult, "Managed server didn't initialize after 1000 ms."),
                Logger);

            if (DeployerSelector.HasNewHandler)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Contains("500.37", responseContent);
            }
        }
    }

    [ConditionalFact]
    public async Task StartupTimeoutIsApplied_DisableRecycleOnStartupTimeout()
    {
        // From what we can tell, this failure is due to ungraceful shutdown.
        // The error could be the same as https://github.com/dotnet/core-setup/issues/4646
        // But can't be certain without another repro.
        using (AppVerifier.Disable(DeployerSelector.ServerType, 0x300))
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
            deploymentParameters.TransformArguments((a, _) => $"{a} Hang");
            deploymentParameters.WebConfigActionList.Add(
                WebConfigHelpers.AddOrModifyAspNetCoreSection("startupTimeLimit", "1"));
            deploymentParameters.HandlerSettings["suppressRecycleOnStartupTimeout"] = "true";
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer(gracefulShutdown: false);

            await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult,
                EventLogHelpers.InProcessFailedToStart(deploymentResult, "Managed server didn't initialize after 1000 ms."),
                Logger);

            if (DeployerSelector.HasNewHandler)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Contains("500.37", responseContent);
            }
        }
    }

    [ConditionalFact]
    public async Task CheckInvalidHostingModelParameter()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("hostingModel", "bogus"));

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        await EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.ConfigurationLoadError(deploymentResult, "Unknown hosting model 'bogus'. Please specify either hostingModel=\"inprocess\" or hostingModel=\"outofprocess\" in the web.config file.")
            );
    }

    private static Dictionary<string, (string, Action<XElement>)> InvalidConfigTransformations = InitInvalidConfigTransformations();
    public static IEnumerable<object[]> InvalidConfigTransformationsScenarios => InvalidConfigTransformations.ToTheoryData();

    [ConditionalTheory]
    [MemberData(nameof(InvalidConfigTransformationsScenarios))]
    public async Task ReportsWebConfigAuthoringErrors(string scenario)
    {
        var (expectedError, action) = InvalidConfigTransformations[scenario];
        var iisDeploymentParameters = Fixture.GetBaseDeploymentParameters();
        iisDeploymentParameters.WebConfigActionList.Add((element, _) => action(element));
        var deploymentResult = await DeployAsync(iisDeploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);

        // Config load errors might not allow us to initialize log file
        deploymentResult.AllowNoLogs();

        StopServer();

        await EventLogHelpers.VerifyEventLogEvents(deploymentResult,
            EventLogHelpers.ConfigurationLoadError(deploymentResult, expectedError)
        );
    }

    public static Dictionary<string, (string, Action<XElement>)> InitInvalidConfigTransformations()
    {
        var dictionary = new Dictionary<string, (string, Action<XElement>)>();
        dictionary.Add("Empty process path",
            (
                "Attribute 'processPath' is required.",
                element => element.Descendants("aspNetCore").Single().SetAttributeValue("processPath", "")
            ));
        dictionary.Add("Unknown hostingModel",
            (
                "Unknown hosting model 'asdf'.",
                element => element.Descendants("aspNetCore").Single().SetAttributeValue("hostingModel", "asdf")
            ));
        dictionary.Add("environmentVariables with add",
            (
                "Unable to get required configuration section 'system.webServer/aspNetCore'. Possible reason is web.config authoring error.",
                element => element.Descendants("aspNetCore").Single().GetOrAdd("environmentVariables").GetOrAdd("add")
            ));
        return dictionary;
    }

    private static Dictionary<string, Func<IISDeploymentParameters, string>> PortableConfigTransformations = InitPortableWebConfigTransformations();
    public static IEnumerable<object[]> PortableConfigTransformationsScenarios => PortableConfigTransformations.ToTheoryData();

    [ConditionalTheory]
    [MemberData(nameof(PortableConfigTransformationsScenarios))]
    public async Task StartsWithWebConfigVariationsPortable(string scenario)
    {
        var action = PortableConfigTransformations[scenario];
        var iisDeploymentParameters = Fixture.GetBaseDeploymentParameters();
        var expectedArguments = action(iisDeploymentParameters);
        var result = await DeployAsync(iisDeploymentParameters);
        Assert.Equal(expectedArguments, await result.HttpClient.GetStringAsync("/CommandLineArgs"));
    }

    public static Dictionary<string, Func<IISDeploymentParameters, string>> InitPortableWebConfigTransformations()
    {
        var dictionary = new Dictionary<string, Func<IISDeploymentParameters, string>>();
        var pathWithSpace = "\u03c0 \u2260 3\u00b714";

        dictionary.Add("App in bin subdirectory full path to dll using exec and quotes",
            parameters =>
            {
                MoveApplication(parameters, "bin");
                parameters.TransformArguments((arguments, root) => "exec " + Path.Combine(root, "bin", arguments));
                return "";
            });

        dictionary.Add("App in subdirectory with space",
            parameters =>
            {
                MoveApplication(parameters, pathWithSpace);
                parameters.TransformArguments((arguments, root) => Path.Combine(pathWithSpace, arguments));
                return "";
            });

        dictionary.Add("App in subdirectory with space and full path to dll",
            parameters =>
            {
                MoveApplication(parameters, pathWithSpace);
                parameters.TransformArguments((arguments, root) => Path.Combine(root, pathWithSpace, arguments));
                return "";
            });

        dictionary.Add("App in bin subdirectory with space full path to dll using exec and quotes",
            parameters =>
            {
                MoveApplication(parameters, pathWithSpace);
                parameters.TransformArguments((arguments, root) => "exec \"" + Path.Combine(root, pathWithSpace, arguments) + "\" extra arguments");
                return "extra|arguments";
            });

        dictionary.Add("App in bin subdirectory and quoted argument",
            parameters =>
            {
                MoveApplication(parameters, "bin");
                parameters.TransformArguments((arguments, root) => Path.Combine("bin", arguments) + " \"extra argument\"");
                return "extra argument";
            });

        dictionary.Add("App in bin subdirectory full path to dll",
            parameters =>
            {
                MoveApplication(parameters, "bin");
                parameters.TransformArguments((arguments, root) => Path.Combine(root, "bin", arguments) + " extra arguments");
                return "extra|arguments";
            });
        return dictionary;
    }

    private static Dictionary<string, Func<IISDeploymentParameters, string>> StandaloneConfigTransformations = InitStandaloneConfigTransformations();

    public static IEnumerable<object[]> StandaloneConfigTransformationsScenarios => StandaloneConfigTransformations.ToTheoryData();

    [ConditionalTheory]
    [MemberData(nameof(StandaloneConfigTransformationsScenarios))]
    public async Task StartsWithWebConfigVariationsStandalone(string scenario)
    {
        var action = StandaloneConfigTransformations[scenario];
        var iisDeploymentParameters = Fixture.GetBaseDeploymentParameters();
        iisDeploymentParameters.ApplicationType = ApplicationType.Standalone;
        var expectedArguments = action(iisDeploymentParameters);
        var result = await DeployAsync(iisDeploymentParameters);
        Assert.Equal(expectedArguments, await result.HttpClient.GetStringAsync("/CommandLineArgs"));
    }

    public static Dictionary<string, Func<IISDeploymentParameters, string>> InitStandaloneConfigTransformations()
    {
        var dictionary = new Dictionary<string, Func<IISDeploymentParameters, string>>();
        var pathWithSpace = "\u03c0 \u2260 3\u00b714";

        dictionary.Add("App in subdirectory",
            parameters =>
            {
                MoveApplication(parameters, pathWithSpace);
                parameters.TransformPath((path, root) => Path.Combine(pathWithSpace, path));
                parameters.TransformArguments((arguments, root) => "\"additional argument\"");
                return "additional argument";
            });

        dictionary.Add("App in bin subdirectory full path",
            parameters =>
            {
                MoveApplication(parameters, pathWithSpace);
                parameters.TransformPath((path, root) => Path.Combine(root, pathWithSpace, path));
                parameters.TransformArguments((arguments, root) => "additional arguments");
                return "additional|arguments";
            });

        return dictionary;
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task SetCurrentDirectoryHandlerSettingWorks()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["SetCurrentDirectory"] = "false";

        var deploymentResult = await DeployAsync(deploymentParameters);

        Assert.Equal(deploymentResult.ContentRoot, await deploymentResult.HttpClient.GetStringAsync("/ContentRootPath"));
        Assert.Equal(deploymentResult.ContentRoot + "\\wwwroot", await deploymentResult.HttpClient.GetStringAsync("/WebRootPath"));
        Assert.Equal(Path.GetDirectoryName(deploymentResult.HostProcess.MainModule.FileName), await deploymentResult.HttpClient.GetStringAsync("/CurrentDirectory"));
        Assert.Equal(deploymentResult.ContentRoot + "\\", await deploymentResult.HttpClient.GetStringAsync("/BaseDirectory"));
        Assert.Equal(deploymentResult.ContentRoot + "\\", await deploymentResult.HttpClient.GetStringAsync("/ASPNETCORE_IIS_PHYSICAL_PATH"));
    }

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task StartupIsSuspendedWhenEventIsUsed()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.ApplicationType = ApplicationType.Standalone;
        deploymentParameters.EnvironmentVariables["ASPNETCORE_STARTUP_SUSPEND_EVENT"] = "ANCM_TestEvent";

        var eventPrefix = deploymentParameters.ServerType == ServerType.IISExpress ? "" : "Global\\";

        var startWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, eventPrefix + "ANCM_TestEvent");
        var suspendedWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, eventPrefix + "ANCM_TestEvent_suspended");

        var deploymentResult = await DeployAsync(deploymentParameters);

        var request = deploymentResult.AssertStarts();

        Assert.True(suspendedWaitHandle.WaitOne(TimeoutExtensions.DefaultTimeoutValue));

        // didn't figure out a better way to check that ANCM is waiting to start
        var applicationDll = Path.Combine(deploymentResult.ContentRoot, "InProcessWebSite.dll");
        var handlerDll = Path.Combine(deploymentResult.ContentRoot, "aspnetcorev2_inprocess.dll");
        // Make sure application dll is not locked
        File.WriteAllBytes(applicationDll, File.ReadAllBytes(applicationDll));
        // Make sure handler dll is not locked
        File.WriteAllBytes(handlerDll, File.ReadAllBytes(handlerDll));
        // Make sure request is not completed
        Assert.False(request.IsCompleted);

        startWaitHandle.Set();

        await request;
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewHandler]
    [InlineData("ASPNETCORE_ENVIRONMENT", "Development")]
    [InlineData("DOTNET_ENVIRONMENT", "deVelopment")]
    [InlineData("ASPNETCORE_DETAILEDERRORS", "1")]
    [InlineData("ASPNETCORE_DETAILEDERRORS", "TRUE")]
    public async Task ExceptionIsLoggedToEventLogAndPutInResponseWhenDeveloperExceptionPageIsEnabled(string environmentVariable, string value)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} Throw");

        // Deployment parameters by default set ASPNETCORE_DETAILEDERRORS to true
        deploymentParameters.EnvironmentVariables.Remove("ASPNETCORE_DETAILEDERRORS");
        deploymentParameters.EnvironmentVariables[environmentVariable] = value;

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.False(result.IsSuccessStatusCode);

        var content = await result.Content.ReadAsStringAsync();
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("TestSite.Program.Main", content);

        StopServer();

        VerifyDotnetRuntimeEventLog(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ExceptionIsLoggedToEventLogAndPutInResponseWhenDeveloperExceptionPageIsEnabledViaWebConfig()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} Throw");

        // Deployment parameters by default set ASPNETCORE_DETAILEDERRORS to true
        deploymentParameters.EnvironmentVariables.Remove("ASPNETCORE_DETAILEDERRORS");
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "TRUE";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.False(result.IsSuccessStatusCode);

        var content = await result.Content.ReadAsStringAsync();
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("TestSite.Program.Main", content);

        StopServer();

        VerifyDotnetRuntimeEventLog(deploymentResult);
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewHandler]
    [InlineData("ThrowInStartup")]
    [InlineData("ThrowInStartupGenericHost")]
    public async Task ExceptionIsLoggedToEventLogAndPutInResponseDuringHostingStartupProcess(string startupType)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} {startupType}");

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.False(result.IsSuccessStatusCode);

        var content = await result.Content.ReadAsStringAsync();
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("TestSite.Program.Main", content);
        Assert.Contains("From Configure", content);

        StopServer();

        VerifyDotnetRuntimeEventLog(deploymentResult);
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewHandler]
    public async Task ExceptionIsNotLoggedToResponseWhenStartupHookIsDisabled()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} Throw");
        deploymentParameters.EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "";
        deploymentParameters.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
        deploymentParameters.HandlerSettings["callStartupHook"] = "false";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.False(result.IsSuccessStatusCode);

        var content = await result.Content.ReadAsStringAsync();
        Assert.DoesNotContain("InvalidOperationException", content);

        StopServer();

        VerifyDotnetRuntimeEventLog(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ExceptionIsLoggedToEventLogDoesNotWriteToResponse()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} Throw");

        // Deployment parameters by default set ASPNETCORE_DETAILEDERRORS to true
        deploymentParameters.EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/");
        Assert.False(result.IsSuccessStatusCode);

        var content = await result.Content.ReadAsStringAsync();
        Assert.DoesNotContain("InvalidOperationException", content);

        StopServer();

        VerifyDotnetRuntimeEventLog(deploymentResult);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task CanAddCustomStartupHook()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        // Deployment parameters by default set ASPNETCORE_DETAILEDERRORS to true
        deploymentParameters.WebConfigBasedEnvironmentVariables["DOTNET_STARTUP_HOOKS"] = "InProcessWebSite";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/StartupHook");
        var content = await result.Content.ReadAsStringAsync();
        Assert.Equal("True", content);

        StopServer();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task CanAddCustomStartupHookWhenIISOneIsDisabled()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();

        // Deployment parameters by default set ASPNETCORE_DETAILEDERRORS to true
        deploymentParameters.WebConfigBasedEnvironmentVariables["DOTNET_STARTUP_HOOKS"] = "InProcessWebSite";
        deploymentParameters.HandlerSettings["callStartupHook"] = "false";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/StartupHook");
        var content = await result.Content.ReadAsStringAsync();
        Assert.Equal("True", content);

        StopServer();
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task StackOverflowIsAvoidedBySettingLargerStack()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/StackSize");
        Assert.True(result.IsSuccessStatusCode);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task StackOverflowCanBeSetBySettingLargerStackViaHandlerSetting()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.HandlerSettings["stackSize"] = "10000000";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var result = await deploymentResult.HttpClient.GetAsync("/StackSizeLarge");
        Assert.True(result.IsSuccessStatusCode);
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewShim]
    [RequiresNewHandler]
    [InlineData(HostingModel.InProcess)]
    [InlineData(HostingModel.OutOfProcess)]
    public async Task EnvironmentVariableForLauncherPathIsPreferred(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);

        deploymentParameters.EnvironmentVariables["ANCM_LAUNCHER_PATH"] = _dotnetLocation;
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "nope"));

        await StartAsync(deploymentParameters);
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewShim]
    [RequiresNewHandler]
    [InlineData(HostingModel.InProcess)]
    [InlineData(HostingModel.OutOfProcess)]
    public async Task EnvironmentVariableForLauncherArgsIsPreferred(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        using var publishedApp = await deploymentParameters.ApplicationPublisher.Publish(deploymentParameters, LoggerFactory.CreateLogger("test"));

        deploymentParameters.EnvironmentVariables["ANCM_LAUNCHER_ARGS"] = Path.ChangeExtension(Path.Combine(publishedApp.Path, deploymentParameters.ApplicationPublisher.ApplicationPath), ".dll");
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("arguments", "nope"));

        await StartAsync(deploymentParameters);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task OnCompletedDoesNotFailRequest()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/OnCompletedThrows");
        Assert.True(response.IsSuccessStatusCode);

        StopServer();

        if (deploymentParameters.ServerType == ServerType.IISExpress)
        {
            // We can't read stdout logs from IIS as they aren't redirected.
            Assert.Contains(TestSink.Writes, context => context.Message.Contains("An unhandled exception was thrown by the application."));
        }
    }

    [ConditionalTheory]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52734")]
    [InlineData("CheckLargeStdErrWrites")]
    [InlineData("CheckLargeStdOutWrites")]
    [InlineData("CheckOversizedStdErrWrites")]
    [InlineData("CheckOversizedStdOutWrites")]
    public async Task CheckStdoutWithLargeWrites_TestSink(string mode)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} {mode}");
        var deploymentResult = await DeployAsync(deploymentParameters);

        await AssertFailsToStart(deploymentResult);

        StopServer();

        // Logs can be split due to the ANCM logging occuring at the same time as logs from the app, so check for a portion of the
        // string instead of the entire string. The entire string will still be present in the event log.
        var expectedLogString = new string('a', 16);

        Assert.Contains(TestSink.Writes, context => context.Message.Contains(expectedLogString));
        var expectedEventLogString = new string('a', 30000);

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessThreadExitStdOut(deploymentResult, "12", expectedEventLogString), Logger);
    }

    [ConditionalTheory]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/58108")]
    [InlineData("CheckLargeStdOutWrites")]
    [InlineData("CheckOversizedStdOutWrites")]
    public async Task CheckStdoutWithLargeWrites_LogFile(string mode)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} {mode}");
        deploymentParameters.EnableLogging(LogFolderPath);

        var deploymentResult = await DeployAsync(deploymentParameters);

        await AssertFailsToStart(deploymentResult);

        var contents = GetLogFileContent(deploymentResult);

        // Logs can be split due to the ANCM logging occuring at the same time as logs from the app, so check for a portion of the
        // string instead of the entire string. The entire string will still be present in the event log.
        var expectedLogString = new string('a', 16);

        Assert.Contains(expectedLogString, contents);

        var expectedEventLogString = new string('a', 30000);

        await EventLogHelpers.VerifyEventLogEventAsync(deploymentResult, EventLogHelpers.InProcessThreadExitStdOut(deploymentResult, "12", expectedEventLogString), Logger);
    }

    [ConditionalFact]
    public async Task CheckValidConsoleFunctions()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} CheckConsoleFunctions");

        var deploymentResult = await DeployAsync(deploymentParameters);

        await AssertFailsToStart(deploymentResult);

        Assert.Contains(TestSink.Writes, context => context.Message.Contains("Is Console redirection: True"));
    }

    [ConditionalFact]
    public async Task Gets500_30_ErrorPage()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);
        deploymentParameters.TransformArguments((a, _) => $"{a} EarlyReturn");

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains("500.30", responseText);
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task IncludesAdditionalErrorPageTextInProcessHandlerLoadFailure_CorrectString()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var response = await DeployAppWithStartupFailure(deploymentParameters);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("500.0", responseString);
        VerifyNoExtraTrailingBytes(responseString);

        await AssertLink(response);
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewShim]
    public async Task IncludesAdditionalErrorPageTextOutOfProcessStartupFailure_CorrectString()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        var response = await DeployAppWithStartupFailure(deploymentParameters);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        StopServer();

        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("HTTP Error 502.5 - ANCM Out-Of-Process Startup Failure", responseString);
        VerifyNoExtraTrailingBytes(responseString);

        await AssertLink(response);
    }

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public async Task IncludesAdditionalErrorPageTextOutOfProcessHandlerLoadFailure_CorrectString()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.HandlerSettings["handlerVersion"] = "88.93";
        deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("500.0", responseString);
        VerifyNoExtraTrailingBytes(responseString);

        await AssertLink(response);
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewHandler]
    public async Task IncludesAdditionalErrorPageTextInProcessStartupFailure_CorrectString()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} EarlyReturn");
        deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var response = await deploymentResult.HttpClient.GetAsync("HelloWorld");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();

        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("500.30", responseString);
        VerifyNoExtraTrailingBytes(responseString);

        await AssertLink(response);
    }

    [ConditionalFact]
    public async Task GetLongEnvironmentVariable_InProcess()
    {
        var expectedValue = "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative";

        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"] = expectedValue;

        Assert.Equal(
            expectedValue,
            await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"));
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task GetLongEnvironmentVariable_OutOfProcess()
    {
        var expectedValue = "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                            "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative";

        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"] = expectedValue;

        Assert.Equal(
            expectedValue,
            await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_INPROCESS_TESTING_LONG_VALUE"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public Task AuthHeaderEnvironmentVariableRemoved_InProcess() => AuthHeaderEnvironmentVariableRemoved(HostingModel.InProcess);

    [ConditionalFact]
    public Task AuthHeaderEnvironmentVariableRemoved_OutOfProcess() => AuthHeaderEnvironmentVariableRemoved(HostingModel.OutOfProcess);

    private async Task AuthHeaderEnvironmentVariableRemoved(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_IIS_HTTPAUTH"] = "shouldberemoved";

        Assert.DoesNotContain("shouldberemoved", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_IIS_HTTPAUTH"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigOverridesGlobalEnvironmentVariables_InProcess() => WebConfigOverridesGlobalEnvironmentVariables(HostingModel.InProcess);

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigOverridesGlobalEnvironmentVariables_OutOfProcess() => WebConfigOverridesGlobalEnvironmentVariables(HostingModel.OutOfProcess);

    private async Task WebConfigOverridesGlobalEnvironmentVariables(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        deploymentParameters.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";
        Assert.Equal("Production", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_ENVIRONMENT"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigAppendsHostingStartup_InProcess() => WebConfigAppendsHostingStartup(HostingModel.InProcess);

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigAppendsHostingStartup_OutOfProcess() => WebConfigAppendsHostingStartup(HostingModel.OutOfProcess);

    private async Task WebConfigAppendsHostingStartup(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        deploymentParameters.EnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm1";
        if (hostingModel == HostingModel.InProcess)
        {
            Assert.Equal("Asm1", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
        }
        else
        {
            Assert.Equal("Asm1;Microsoft.AspNetCore.Server.IISIntegration", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigOverridesHostingStartup_InProcess() => WebConfigOverridesHostingStartup(HostingModel.InProcess);

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigOverridesHostingStartup_OutOfProcess() => WebConfigOverridesHostingStartup(HostingModel.OutOfProcess);

    private async Task WebConfigOverridesHostingStartup(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        deploymentParameters.EnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm1";
        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Asm2";
        Assert.Equal("Asm2", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigExpandsVariables_InProcess() => WebConfigExpandsVariables(HostingModel.InProcess);

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    public Task WebConfigExpandsVariables_OutOfProcess() => WebConfigExpandsVariables(HostingModel.OutOfProcess);

    private async Task WebConfigExpandsVariables(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
        deploymentParameters.EnvironmentVariables["TestVariable"] = "World";
        deploymentParameters.WebConfigBasedEnvironmentVariables["OtherVariable"] = "%TestVariable%;Hello";
        Assert.Equal("World;Hello", await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=OtherVariable"));
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
    [RequiresNewHandler]
    [RequiresNewShim]
    [InlineData(HostingModel.InProcess)]
    [InlineData(HostingModel.OutOfProcess)]
    public async Task PreferEnvironmentVariablesOverWebConfigWhenConfigured(HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);

        var environment = "Development";
        deploymentParameters.EnvironmentVariables["ANCM_PREFER_ENVIRONMENT_VARIABLES"] = "true";
        deploymentParameters.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
        deploymentParameters.WebConfigBasedEnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Debug");
        Assert.Equal(environment, await GetStringAsync(deploymentParameters, "/GetEnvironmentVariable?name=ASPNETCORE_ENVIRONMENT"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresNewShim]
    [SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;" + "Windows.Amd64.VS2022.Pre;")]
    public async Task ServerAddressesIncludesBaseAddress()
    {
        var appName = "\u041C\u043E\u0451\u041F\u0440\u0438\u043B\u043E\u0436\u0435\u043D\u0438\u0435";

        var port = TestPortHelper.GetNextSSLPort();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
        deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
        deploymentParameters.AddHttpsToServerConfig();
        deploymentParameters.SetWindowsAuth(false);
        deploymentParameters.AddServerConfigAction(
            (element, root) =>
            {
                element.Descendants("site").Single().Element("application").SetAttributeValue("path", "/" + appName);
                Helpers.CreateEmptyApplication(element, root);
            });

        var deploymentResult = await DeployAsync(deploymentParameters);
        var client = CreateNonValidatingClient(deploymentResult);
        Assert.Equal(deploymentParameters.ApplicationBaseUriHint + appName, await client.GetStringAsync($"/{appName}/ServerAddresses"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresNewShim]
    public async Task AncmHttpsPortCanBeOverriden()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

        deploymentParameters.AddServerConfigAction(
            element =>
            {
                element.Descendants("bindings")
                    .Single()
                    .GetOrAdd("binding", "protocol", "https")
                    .SetAttributeValue("bindingInformation", $":{TestPortHelper.GetNextSSLPort()}:localhost");
            });

        deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_ANCM_HTTPS_PORT"] = "123";

        var deploymentResult = await DeployAsync(deploymentParameters);
        var client = CreateNonValidatingClient(deploymentResult);

        Assert.Equal("123", await client.GetStringAsync("/ANCM_HTTPS_PORT"));
        Assert.Equal("NOVALUE", await client.GetStringAsync("/HTTPS_PORT"));
    }

    [ConditionalFact]
    [RequiresNewShim]
    public async Task HttpsRedirectionWorksIn30AndNot22()
    {
        var port = TestPortHelper.GetNextSSLPort();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
        deploymentParameters.WebConfigBasedEnvironmentVariables["ENABLE_HTTPS_REDIRECTION"] = "true";
        deploymentParameters.ApplicationBaseUriHint = $"http://localhost:{TestPortHelper.GetNextPort()}/";

        deploymentParameters.AddServerConfigAction(
            element =>
            {
                element.Descendants("bindings")
                    .Single()
                    .AddAndGetInnerElement("binding", "protocol", "https")
                    .SetAttributeValue("bindingInformation", $":{port}:localhost");

                element.Descendants("access")
                    .Single()
                    .SetAttributeValue("sslFlags", "None");
            });

        var deploymentResult = await DeployAsync(deploymentParameters);
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
            AllowAutoRedirect = false
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(deploymentParameters.ApplicationBaseUriHint),
            Timeout = TimeSpan.FromSeconds(200),
        };

        if (DeployerSelector.HasNewHandler)
        {
            var response = await client.GetAsync("/ANCM_HTTPS_PORT");
            Assert.Equal(307, (int)response.StatusCode);
        }
        else
        {
            var response = await client.GetAsync("/ANCM_HTTPS_PORT");
            Assert.Equal(200, (int)response.StatusCode);
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresNewShim]
    public async Task MultipleHttpsPortsProduceNoEnvVar()
    {
        var sslPort = GetNextSSLPort();
        var anotherSslPort = GetNextSSLPort(sslPort);

        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

        deploymentParameters.AddServerConfigAction(
            element =>
            {
                element.Descendants("bindings")
                    .Single()
                    .Add(
                        new XElement("binding",
                            new XAttribute("protocol", "https"),
                            new XAttribute("bindingInformation", $":{sslPort}:localhost")),
                        new XElement("binding",
                            new XAttribute("protocol", "https"),
                            new XAttribute("bindingInformation", $":{anotherSslPort}:localhost")));
            });

        var deploymentResult = await DeployAsync(deploymentParameters);
        var client = CreateNonValidatingClient(deploymentResult);

        Assert.Equal("NOVALUE", await client.GetStringAsync("/ANCM_HTTPS_PORT"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresNewShim]
    public async Task SetsConnectionCloseHeader()
    {
        // Only tests OutOfProcess as the Connection header is removed for out of process and not inprocess.
        // This test checks a quirk to allow setting the Connection header.
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

        deploymentParameters.HandlerSettings["forwardResponseConnectionHeader"] = "true";
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("ConnectionClose");
        Assert.Equal(true, response.Headers.ConnectionClose);
    }

    public static int GetNextSSLPort(int avoid = 0)
    {
        var next = 44300;
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            while (true)
            {
                try
                {
                    var port = next++;
                    if (port == avoid)
                    {
                        continue;
                    }
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                    return port;
                }
                catch (SocketException)
                {
                    // Retry unless exhausted
                    if (next > 44399)
                    {
                        throw;
                    }
                }
            }
        }
    }

    private static HttpClient CreateNonValidatingClient(IISDeploymentResult deploymentResult)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true
        };
        return deploymentResult.CreateClient(handler);
    }

    private static void VerifyNoExtraTrailingBytes(string responseString)
    {
        if (DeployerSelector.HasNewShim)
        {
            Assert.EndsWith("</html>\r\n", responseString);
        }
    }

    private static async Task AssertLink(HttpResponseMessage response)
    {
        Assert.Contains("<a href=\"http://example\"> <cite> http://example </cite></a> and ", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> DeployAppWithStartupFailure(IISDeploymentParameters deploymentParameters)
    {
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "doesnot"));
        deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("arguments", "start"));

        deploymentParameters.EnvironmentVariables["ANCM_ADDITIONAL_ERROR_PAGE_LINK"] = "http://example";

        var deploymentResult = await DeployAsync(deploymentParameters);

        return await deploymentResult.HttpClient.GetAsync("HelloWorld");
    }

    private async Task AssertFailsToStart(IISDeploymentResult deploymentResult)
    {
        var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        StopServer();
    }

    private static void VerifyDotnetRuntimeEventLog(IISDeploymentResult deploymentResult)
    {
        var entries = GetEventLogsFromDotnetRuntime(deploymentResult);

        var expectedRegex = new Regex("Exception Info: System\\.InvalidOperationException:", RegexOptions.Singleline);
        var matchedEntries = entries.Where(entry => expectedRegex.IsMatch(entry.Message)).ToArray();
        // There isn't a process ID to filter on here, so there can be duplicate entries from other tests.
        Assert.True(matchedEntries.Length > 0);
    }

    private static IEnumerable<EventLogEntry> GetEventLogsFromDotnetRuntime(IISDeploymentResult deploymentResult)
    {
        var processStartTime = deploymentResult.HostProcess.StartTime.AddSeconds(-5);
        var eventLog = new EventLog("Application");

        for (var i = eventLog.Entries.Count - 1; i >= 0; i--)
        {
            var eventLogEntry = eventLog.Entries[i];
            if (eventLogEntry.TimeGenerated < processStartTime)
            {
                // If event logs is older than the process start time, we didn't find a match.
                break;
            }

            if (eventLogEntry.ReplacementStrings == null)
            {
                continue;
            }

            if (eventLogEntry.Source == ".NET Runtime")
            {
                yield return eventLogEntry;
            }
        }
    }

    private static void MoveApplication(
        IISDeploymentParameters parameters,
        string subdirectory)
    {
        parameters.WebConfigActionList.Add((config, contentRoot) =>
        {
            var source = new DirectoryInfo(contentRoot);
            var subDirectoryPath = source.CreateSubdirectory(subdirectory);

            // Copy everything into a subfolder
            Helpers.CopyFiles(source, subDirectoryPath, null);
            // Cleanup files
            foreach (var fileSystemInfo in source.GetFiles())
            {
                fileSystemInfo.Delete();
            }
        });
    }

    private Task AssertSiteFailsToStartWithInProcessStaticContent(IISDeploymentResult deploymentResult)
    {
        return AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.0");
    }

    private async Task AssertSiteFailsToStartWithInProcessStaticContent(IISDeploymentResult deploymentResult, string error)
    {
        HttpResponseMessage response = null;

        // Make sure strings aren't freed.
        for (var i = 0; i < 2; i++)
        {
            response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        }

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        StopServer();
    }

    private async Task AssertSiteFailsToStartWithInProcessStaticContent(IISDeploymentResult deploymentResult, params string[] errors)
    {
        HttpResponseMessage response = null;

        // Make sure strings aren't freed.
        for (var i = 0; i < 2; i++)
        {
            response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
        }

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync();
        foreach (var error in errors)
        {
            Assert.Contains(error, responseText);
        }
        StopServer();
    }
}
