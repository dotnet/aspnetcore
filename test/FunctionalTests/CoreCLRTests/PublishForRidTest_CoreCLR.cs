// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class PublishWithRidTest_CoreCLR : LoggedTest
    {
        public PublishWithRidTest_CoreCLR(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void CrossPublishingWorks()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var applicationName = nameof(SimpleApp);
                var applicationPath = ApplicationPaths.GetTestAppDirectory(applicationName);
                var deploymentParameters = ApplicationTestFixture.GetDeploymentParameters(
                    applicationPath,
                    applicationName,
                    RuntimeFlavor.CoreClr,
#if NETCOREAPP2_0
            "netcoreapp2.0");
#elif NETCOREAPP2_1
            "netcoreapp2.1");
#else
#error Target frameworks need to be updated
#endif

                // Deploy for a rid that does not exist on the current platform.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    deploymentParameters.AdditionalPublishParameters = "-r debian-x64";
                }
                else
                {
                    deploymentParameters.AdditionalPublishParameters = "-r win7-x86";
                }
                var deployer = new DotNetPublishDeployer(deploymentParameters, loggerFactory);

                // Act
                deployer.DotnetPublish();

                // Act
                var expectedFile = Path.Combine(
                    deploymentParameters.PublishedApplicationRootPath,
                    $"{applicationName}.PrecompiledViews.dll");
                Assert.True(File.Exists(expectedFile), $"Expected precompiled file {expectedFile} does not exist.");
            }
        }

        private class DotNetPublishDeployer : ApplicationDeployer
        {
            public DotNetPublishDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
                : base(deploymentParameters, loggerFactory)
            {
            }

            public void DotnetPublish() => base.DotnetPublish();

            public override void Dispose() => CleanPublishedOutput();

            public override Task<DeploymentResult> DeployAsync() => throw new NotSupportedException();
        }
    }
}
