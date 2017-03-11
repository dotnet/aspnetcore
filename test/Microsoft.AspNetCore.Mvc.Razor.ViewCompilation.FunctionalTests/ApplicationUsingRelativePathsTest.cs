// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ApplicationUsingRelativePathsTest :
        IClassFixture<ApplicationUsingRelativePathsTest.ApplicationUsingRelativePathsTestFixture>
    {
        public ApplicationUsingRelativePathsTest(ApplicationUsingRelativePathsTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_WorksForViewsUsingRelativePath()
        {
            // Arrange
            var deploymentResult = Fixture.CreateDeployment();

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                deploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("ApplicationUsingRelativePaths.Home.Index.txt", response);
        }

        [Fact]
        public async Task Precompilation_WorksForViewsUsingDirectoryTraversal()
        {
            // Arrange
            var deploymentResult = Fixture.CreateDeployment();

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                deploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("ApplicationUsingRelativePaths.Home.About.txt", response);
        }

        public class ApplicationUsingRelativePathsTestFixture : ApplicationTestFixture
        {
            public ApplicationUsingRelativePathsTestFixture()
                : base("ApplicationUsingRelativePaths")
            {
            }
        }
    }
}
