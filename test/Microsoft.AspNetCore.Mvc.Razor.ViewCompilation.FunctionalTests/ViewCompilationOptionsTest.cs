// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/115")]
        public void Precompilation_PreventsRefAssembliesFromBeingPublished()
        {
            // Act & Assert
            Assert.False(Directory.Exists(Path.Combine(Fixture.DeploymentResult.ContentRoot, "refs")));
        }

        [Fact]
        public async Task PublishingWithOption_AllowsPublishingRefAssemblies()
        {
            // Arrange
            var deploymentParameters = Fixture.GetDeploymentParameters();
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

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleApp")
            {
            }
        }
    }
}
