// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
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
public class ApplicationInitializationTests : IISFunctionalTestBase
{
    public ApplicationInitializationTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.ApplicationInitialization)]
    [InlineData(HostingModel.InProcess, true)]
    [InlineData(HostingModel.OutOfProcess, true)]
    [InlineData(HostingModel.InProcess, false)]
    [InlineData(HostingModel.OutOfProcess, false)]
    public async Task ApplicationPreloadStartsApp(HostingModel hostingModel, bool delayShutdown)
    {
        // This test often hits a memory leak in warmup.dll module, it has been reported to IIS team
        using (AppVerifier.Disable(DeployerSelector.ServerType, 0x900))
        {
            var baseDeploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            baseDeploymentParameters.TransformArguments(
                (args, contentRoot) => $"{args} CreateFile \"{Path.Combine(contentRoot, "Started.txt")}\"");
            EnablePreload(baseDeploymentParameters);

            baseDeploymentParameters.HandlerSettings["shutdownDelay"] = delayShutdown ? "1000" : "0";
            var result = await DeployAsync(baseDeploymentParameters);

            await Helpers.Retry(async () => await File.ReadAllTextAsync(Path.Combine(result.ContentRoot, "Started.txt")), TimeoutExtensions.DefaultTimeoutValue);
            StopServer();
            EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.Started(result), Logger);

            if (delayShutdown)
            {
                EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.ShutdownMessage(result), Logger);
            }
            else
            {
                Assert.True(result.HostProcess.HasExited);

                var entries = EventLogHelpers.GetEntries(result);
                var expectedRegex = new Regex(EventLogHelpers.ShutdownMessage(result), RegexOptions.Singleline);
                var matchedEntries = entries.Where(entry => expectedRegex.IsMatch(entry.Message)).ToArray();
                Assert.Empty(matchedEntries);
            }
        }
    }

    [ConditionalTheory]
    [RequiresIIS(IISCapability.ApplicationInitialization)]
    [RequiresNewHandler]
    [InlineData(HostingModel.InProcess)]
    [InlineData(HostingModel.OutOfProcess)]
    public async Task ApplicationInitializationPageIsRequested(HostingModel hostingModel)
    {
        // This test often hits a memory leak in warmup.dll module, it has been reported to IIS team
        using (AppVerifier.Disable(DeployerSelector.ServerType, 0x900))
        {
            var baseDeploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);
            EnablePreload(baseDeploymentParameters);

            baseDeploymentParameters.ServerConfigActionList.Add(
                (config, _) =>
                {
                    config
                        .RequiredElement("system.webServer")
                        .GetOrAdd("applicationInitialization")
                        .GetOrAdd("add", "initializationPage", "/CreateFile");
                });

            var result = await DeployAsync(baseDeploymentParameters);

            await Helpers.Retry(async () => await File.ReadAllTextAsync(Path.Combine(result.ContentRoot, "Started.txt")), TimeoutExtensions.DefaultTimeoutValue);
            StopServer();
            EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.Started(result), Logger);
            EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.ShutdownMessage(result), Logger);
        }
    }

    private static void EnablePreload(IISDeploymentParameters baseDeploymentParameters)
    {
        baseDeploymentParameters.EnsureSection("applicationInitialization", "system.webServer");
        baseDeploymentParameters.ServerConfigActionList.Add(
            (config, _) =>
            {

                config
                    .RequiredElement("system.applicationHost")
                    .RequiredElement("sites")
                    .RequiredElement("site")
                    .RequiredElement("application")
                    .SetAttributeValue("preloadEnabled", true);
            });

        baseDeploymentParameters.EnableModule("ApplicationInitializationModule", "%IIS_BIN%\\warmup.dll");
    }
}
