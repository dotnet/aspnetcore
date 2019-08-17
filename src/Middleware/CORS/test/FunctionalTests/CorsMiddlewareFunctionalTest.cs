// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace FunctionalTests
{
    public class CorsMiddlewareFunctionalTests : LoggedTest
    {
        public CorsMiddlewareFunctionalTests(ITestOutputHelper output)
            : base(output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        [Flaky("https://github.com/aspnet/aspnetcore-internal/issues/2865", FlakyOn.All)]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Disabling this test on OSX until we have a resolution for https://github.com/aspnet/AspNetCore-Internal/issues/1619")]
        [InlineData("Startup")]
        [InlineData("StartupWithoutEndpointRouting")]
        public async Task RunClientTests(string startup)
        {
            using (StartLog(out var loggerFactory))
            using (var deploymentResult = await CreateDeployments(loggerFactory, startup))
            {
                ProcessStartInfo processStartInfo;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = "/c npm test --no-color --no-watchman",
                    };
                }
                else
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "npm",
                        Arguments = "test --no-watchman",
                    };
                }
                // Disallow the test from downloading \ installing chromium.
                processStartInfo.Environment["PUPPETEER_SKIP_CHROMIUM_DOWNLOAD"] = "true";
                processStartInfo.Environment["DESTINATION_PORT"] = deploymentResult.DestinationResult.HttpClient.BaseAddress.Port.ToString();
                processStartInfo.Environment["ORIGIN_PORT"] = deploymentResult.OriginResult.HttpClient.BaseAddress.Port.ToString();
                processStartInfo.Environment["SECOND_ORIGIN_PORT"] = deploymentResult.SecondOriginResult.HttpClient.BaseAddress.Port.ToString();

                // Act
                var result = await ProcessManager.RunProcessAsync(processStartInfo, loggerFactory.CreateLogger("ProcessManager"));

                // Assert
                Assert.Success(result);
                Assert.Contains("Test Suites: 1 passed, 1 total", result.Output);
            }
        }

        private static async Task<CorsDeploymentResult> CreateDeployments(ILoggerFactory loggerFactory, string startup)
        {
            // https://github.com/aspnet/AspNetCore/issues/7990
#pragma warning disable 0618
            var solutionPath = TestPathUtilities.GetSolutionRootDirectory("Middleware");
#pragma warning restore 0618

            var configuration =
#if RELEASE
                "Release";
#else
                "Debug";
#endif

            var originParameters = new DeploymentParameters
            {
                TargetFramework = "netcoreapp3.0",
                RuntimeFlavor = RuntimeFlavor.CoreClr,
                ServerType = ServerType.Kestrel,
                ApplicationPath = Path.Combine(solutionPath, "CORS", "test", "testassets", "TestOrigin"),
                PublishApplicationBeforeDeployment = false,
                ApplicationType = ApplicationType.Portable,
                Configuration = configuration,
            };

            var originFactory = ApplicationDeployerFactory.Create(originParameters, loggerFactory);
            var originDeployment = await originFactory.DeployAsync();

            var secondOriginFactory = ApplicationDeployerFactory.Create(originParameters, loggerFactory);
            var secondOriginDeployment = await secondOriginFactory.DeployAsync();

            var port = originDeployment.HttpClient.BaseAddress.Port;
            var destinationParameters = new DeploymentParameters
            {
                TargetFramework = "netcoreapp3.0",
                RuntimeFlavor = RuntimeFlavor.CoreClr,
                ServerType = ServerType.Kestrel,
                ApplicationPath = Path.Combine(solutionPath, "CORS", "test", "testassets", "TestDestination"),
                PublishApplicationBeforeDeployment = false,
                ApplicationType = ApplicationType.Portable,
                Configuration = configuration,
                EnvironmentVariables =
                {
                    ["CORS_STARTUP"] = startup,
                    ["ORIGIN_PORT"] = port.ToString()
                }
            };

            var destinationFactory = ApplicationDeployerFactory.Create(destinationParameters, loggerFactory);
            var destinationDeployment = await destinationFactory.DeployAsync();

            return new CorsDeploymentResult(originFactory, originDeployment, secondOriginFactory, secondOriginDeployment, destinationFactory, destinationDeployment);
        }

        private readonly struct CorsDeploymentResult : IDisposable
        {
            public CorsDeploymentResult(
                ApplicationDeployer originDeployer,
                DeploymentResult originResult,
                ApplicationDeployer secondOriginDeployer,
                DeploymentResult secondOriginResult,
                ApplicationDeployer destinationDeployer,
                DeploymentResult destinationResult)
            {
                OriginDeployer = originDeployer;
                OriginResult = originResult;
                SecondOriginDeployer = secondOriginDeployer;
                SecondOriginResult = secondOriginResult;
                DestinationDeployer = destinationDeployer;
                DestinationResult = destinationResult;
            }

            public ApplicationDeployer OriginDeployer { get; }

            public DeploymentResult OriginResult { get; }

            public ApplicationDeployer SecondOriginDeployer { get; }

            public DeploymentResult SecondOriginResult { get; }

            public ApplicationDeployer DestinationDeployer { get; }

            public DeploymentResult DestinationResult { get; }

            public void Dispose()
            {
                OriginDeployer.Dispose();
                SecondOriginDeployer.Dispose();
                DestinationDeployer.Dispose();
            }
        }
    }
}
