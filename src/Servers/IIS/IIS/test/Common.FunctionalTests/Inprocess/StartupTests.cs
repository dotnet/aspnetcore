// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(PublishedSitesCollection.Name)]
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

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.UnableToStart(deploymentResult, subError), Logger);
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

            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf("."));
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", dotnetLocationWithoutExtension));

            await StartAsync(deploymentParameters);
        }

        [ConditionalFact]
        public async Task StartsWithDotnetLocationUppercase()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();

            var dotnetLocationWithoutExtension = _dotnetLocation.Substring(0, _dotnetLocation.LastIndexOf(".")).ToUpperInvariant();
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

        [ConditionalTheory]
        [InlineData(RuntimeArchitecture.x64)]
        [InlineData(RuntimeArchitecture.x86)]
        [SkipIfNotAdmin]
        [RequiresNewShim]
        [RequiresIIS(IISCapability.PoolEnvironmentVariables)]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore-internal/issues/2221")]
        public async Task StartsWithDotnetInstallLocation(RuntimeArchitecture runtimeArchitecture)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.RuntimeArchitecture = runtimeArchitecture;

            // IIS doesn't allow empty PATH
            deploymentParameters.EnvironmentVariables["PATH"] = ".";
            deploymentParameters.WebConfigActionList.Add(WebConfigHelpers.AddOrModifyAspNetCoreSection("processPath", "dotnet"));

            // Key is always in 32bit view
            using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                var installDir = DotNetCommands.GetDotNetInstallDir(runtimeArchitecture);
                using (new TestRegistryKey(
                    localMachine,
                    "SOFTWARE\\dotnet\\Setup\\InstalledVersions\\" + runtimeArchitecture,
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

                EventLogHelpers.VerifyEventLogEvent(deploymentResult, "AspNetCore Module is disabled", Logger);
            }
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp50)
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

            // ReferenceTestTasks is workaround for https://github.com/dotnet/sdk/issues/2482
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

            EventLogHelpers.VerifyEventLogEvents(deploymentResult,
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

            EventLogHelpers.VerifyEventLogEvents(deploymentResult,
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

            EventLogHelpers.VerifyEventLogEvents(deploymentResult,
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

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessHostfxrInvalid(deploymentResult), Logger);
        }

        [ConditionalFact]
        public async Task PublishWithWrongBitness()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(Fixture.InProcessTestSite);

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                // TODO skip conditions for IISExpress
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

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessHostfxrUnableToLoad(deploymentResult), Logger);
        }

        [ConditionalFact]
        public async Task TargedDifferenceSharedFramework_FailedToFindNativeDependencies()
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

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessFailedToFindNativeDependencies(deploymentResult), Logger);
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
                Assert.Contains("The framework 'Microsoft.NETCore.App', version '2.9.9'", responseContent);
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

            if (DeployerSelector.HasNewShim && DeployerSelector.HasNewHandler)
            {
                await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.33");

                EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessFailedToFindRequestHandler(deploymentResult), Logger);
            }
            else if (DeployerSelector.HasNewShim)
            {
                // Forwards compat tests fail earlier due to a error with the M.AspNetCore.Server.IIS package.
                await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult, "500.31");

                EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessFailedToFindNativeDependencies(deploymentResult), Logger);
            }
            else
            {
                await AssertSiteFailsToStartWithInProcessStaticContent(deploymentResult);

                EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessFailedToFindRequestHandler(deploymentResult), Logger);
            }
        }

        [ConditionalFact]
        public async Task StartupTimeoutIsApplied()
        {
            // From what I can tell, this failure is due to ungraceful shutdown.
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

                StopServer(gracefulShutdown: false);

                EventLogHelpers.VerifyEventLogEvents(deploymentResult,
                    EventLogHelpers.InProcessFailedToStart(deploymentResult, "Managed server didn't initialize after 1000 ms.")
                    );

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

            EventLogHelpers.VerifyEventLogEvents(deploymentResult,
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

            EventLogHelpers.VerifyEventLogEvents(deploymentResult,
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
    }
}
