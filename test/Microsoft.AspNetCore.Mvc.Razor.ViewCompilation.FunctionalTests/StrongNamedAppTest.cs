// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class StrongNamedAppTest : IClassFixture<StrongNamedAppTest.StrongNamedAppFixture>
    {
        public StrongNamedAppTest(StrongNamedAppFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PrecompiledAssembliesUseSameStrongNameAsApplication()
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment())
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("StrongNamedApp.Home.Index.txt", response);
            }
        }

        public class StrongNamedAppFixture : ApplicationTestFixture
        {
            public StrongNamedAppFixture()
                : base("StrongNamedApp")
            {
            }
        }
    }
}
