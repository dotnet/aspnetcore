// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
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

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForViewsUsingRelativePath(RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.CreateDeployment(flavor);

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("ApplicationUsingRelativePaths.Home.Index.txt", response);
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForViewsUsingDirectoryTraversal(RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.CreateDeployment(flavor);

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
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
