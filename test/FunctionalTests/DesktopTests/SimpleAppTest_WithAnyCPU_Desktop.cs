// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public class SimpleAppTest_WithAnyCPU_Desktop :
        LoggedTest, IClassFixture<SimpleAppTest_WithAnyCPU_Desktop.TestFixture>
    {
        public SimpleAppTest_WithAnyCPU_Desktop(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        public async Task Precompilation_WorksForSimpleApps_BuiltWithPlatformTargetAnyCPU()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                var dllFile = Path.Combine(deployment.ContentRoot, "SimpleApp.PrecompiledViews.dll");
                Assert.True(File.Exists(dllFile), $"{dllFile} exists at deployment.");
            }
        }

        public class TestFixture : DesktopApplicationTestFixture<SimpleApp.Startup>
        {
            public TestFixture()
            {
                PublishOnly = true;
            }

            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.AdditionalPublishParameters = "/p:PlatformTarget=AnyCPU";

                return deploymentParameters;
            }
        }
    }
}
