// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class ViewCompilationOptions_CoreCLR :
        LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<SimpleApp.Startup>>
    {
        public ViewCompilationOptions_CoreCLR(
            CoreCLRApplicationTestFixture<SimpleApp.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_PreventsRefAssembliesFromBeingPublished()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                Assert.False(Directory.Exists(Path.Combine(deployment.ContentRoot, "refs")));
            }
        }

        [Fact]
        public async Task Precompilation_PublishesPdbsToOutputDirectory()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var pdbPath = Path.Combine(deployment.ContentRoot, Fixture.ApplicationName + ".PrecompiledViews.pdb");

                // Act & Assert
                Assert.True(File.Exists(pdbPath), $"PDB at {pdbPath} was not found.");
            }
        }
    }

    public class ViewCompilationOptions_CoreCLR_ScenarioRefAssembliesDoNotGetPublished :
        LoggedTest, IClassFixture<ViewCompilationOptions_CoreCLR_ScenarioRefAssembliesDoNotGetPublished.TestFixture>
    {
        public ViewCompilationOptions_CoreCLR_ScenarioRefAssembliesDoNotGetPublished(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PublishingWithOption_AllowsPublishingRefAssemblies()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                Assert.True(Directory.Exists(Path.Combine(deployment.ContentRoot, "refs")));
            }
        }

        public class TestFixture : CoreCLRApplicationTestFixture<SimpleApp.Startup>
        {
            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.PublishEnvironmentVariables.Add(
                    new KeyValuePair<string, string>("MvcRazorExcludeRefAssembliesFromPublish", "false"));

                return deploymentParameters;
            }
        }
    }
}
