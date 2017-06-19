// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace FunctionalTests
{
    public class ViewCompilationOptionsTest : IClassFixture<ViewCompilationOptionsTest.TestFixture>
    {
        public ViewCompilationOptionsTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_PreventsRefAssembliesFromBeingPublished(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act & Assert
                Assert.False(Directory.Exists(Path.Combine(deployment.DeploymentResult.ContentRoot, "refs")));
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task PublishingWithOption_AllowsPublishingRefAssemblies(RuntimeFlavor flavor)
        {
            // Arrange
            var deploymentParameters = Fixture.GetDeploymentParameters(flavor);
            deploymentParameters.PublishEnvironmentVariables.Add(
                new KeyValuePair<string, string>("MvcRazorExcludeRefAssembliesFromPublish", "false"));

            using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, Fixture.LoggerFactory))
            {
                // Act
                var deploymentResult = await deployer.DeployAsync();

                // Assert
                Assert.True(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "refs")));
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_PublishesPdbsToOutputDirectory(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                var pdbPath = Path.Combine(deployment.DeploymentResult.ContentRoot, Fixture.ApplicationName + ".PrecompiledViews.pdb");

                // Act & Assert
                Assert.True(File.Exists(pdbPath), $"PDB at {pdbPath} was not found.");
            }
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleApp")
            {
            }
        }
    }
}
