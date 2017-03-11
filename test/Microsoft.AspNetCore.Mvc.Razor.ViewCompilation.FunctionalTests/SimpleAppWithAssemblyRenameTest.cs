// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppWithAssemblyRenameTest : IClassFixture<SimpleAppWithAssemblyRenameTest.TestFixture>
    {
        public SimpleAppWithAssemblyRenameTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_WorksForSimpleApps()
        {
            // Arrange
            var deploymentResult = Fixture.CreateDeployment();

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                deploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("SimpleAppWithAssemblyRenameTest.Home.Index.txt", response);
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleAppWithAssemblyRename")
            {
            }

            public override DeploymentParameters GetDeploymentParameters()
            {
                var parameters = base.GetDeploymentParameters();
                parameters.ApplicationName = "NewAssemblyName";
                return parameters;
            }
        }
    }
}
