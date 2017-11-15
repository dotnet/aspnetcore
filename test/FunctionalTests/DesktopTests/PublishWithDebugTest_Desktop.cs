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
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public class PublishWithDebugTest_Desktop :
        LoggedTest, IClassFixture<PublishWithDebugTest_Desktop.TestFixture>
    {
        public PublishWithDebugTest_Desktop(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        [Fact]
        public async Task PublishingInDebugWorks()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Assert
                var expected = Path.Combine(deployment.ContentRoot, $"{Fixture.ApplicationName}.PrecompiledViews.dll");
                Assert.True(File.Exists(expected), $"File {expected} does not exist.");
            }
        }

        public class TestFixture : CoreCLRApplicationTestFixture<SimpleApp.Startup>
        {
            public TestFixture()
            {
                PublishOnly = true;
            }

            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.Configuration = "Debug";

                return deploymentParameters;
            }
        }
    }
}
