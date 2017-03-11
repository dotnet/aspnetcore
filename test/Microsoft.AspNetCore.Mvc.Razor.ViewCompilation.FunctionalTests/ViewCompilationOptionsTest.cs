// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ViewCompilationOptionsTest : IClassFixture<ViewCompilationOptionsTest.TestFixture>
    {
        public ViewCompilationOptionsTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public void Precompilation_PreventsRefAssembliesFromBeingPublished()
        {
            // Arrange & Act
            var deploymentResult = Fixture.CreateDeployment();

            // Assert
            Assert.False(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "refs")));
        }

        [Fact]
        public void PublishingWithOption_AllowsPublishingRefAssemblies()
        {
            // Arrange
            var deploymentParameters = Fixture.GetDeploymentParameters();
            deploymentParameters.PublishEnvironmentVariables.Add(
                new KeyValuePair<string, string>("MvcRazorExcludeRefAssembliesFromPublish", "false"));
            var logger = Fixture.CreateLogger();

            using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
            {
                // Act
                var deploymentResult = deployer.Deploy();

                // Assert
                Assert.True(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "refs")));
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
