// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.FunctionalTests
{
    public class ApplicationWithCustomInputFilesTest
        : IClassFixture<ApplicationWithCustomInputFilesTest.ApplicationWithCustomInputFilesTestFixture>
    {
        private const string ApplicationName = "ApplicationWithCustomInputFiles";

        public ApplicationWithCustomInputFilesTest(ApplicationWithCustomInputFilesTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task ApplicationWithCustomInputFiles_Works()
        {
            var expectedText = "Hello Index!";
            using (var deployer = Fixture.CreateDeployment())
            {
                // Arrange
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                Assert.Equal(expectedText, response.Trim());
            }
        }

        [Fact]
        public async Task MvcRazorFilesToCompile_OverridesTheFilesToBeCompiled()
        {
            // Arrange
            var expectedViews = new[]
            {
                "/Views/Home/About.cshtml",
                "/Views/Home/Index.cshtml",
            };

            using (var deployer = Fixture.CreateDeployment())
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response2 = await Fixture.HttpClient.GetStringWithRetryAsync(
                    $"{deploymentResult.ApplicationBaseUri}Home/GetPrecompiledResourceNames",
                    Fixture.Logger);

                // Assert
                var actual = response2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedViews, actual);
            }
        }

        [Fact]
        public void MvcRazorFilesToCompile_SpecificallyDoesNotPublishFilesToBeCompiled()
        {
            // Arrange
            var viewsNotPublished = new[]
            {
                "Index.cshtml",
                "About.cshtml",
            };

            var viewsPublished = new[]
            {
                "NotIncluded.cshtml",
            };

            using (var deployer = Fixture.CreateDeployment())
            {
                var deploymentResult = deployer.Deploy();
                var viewsDirectory = Path.Combine(deploymentResult.ContentRoot, "Views", "Home");
                
                // Act & Assert
                foreach (var file in viewsPublished)
                {
                    var filePath = Path.Combine(viewsDirectory, file);
                    Assert.True(File.Exists(filePath), $"{filePath} was not published.");
                }

                foreach (var file in viewsNotPublished)
                {
                    var filePath = Path.Combine(viewsDirectory, file);
                    Assert.False(File.Exists(filePath), $"{filePath} was published.");
                }
            }
        }

        public class ApplicationWithCustomInputFilesTestFixture : ApplicationTestFixture
        {
            public ApplicationWithCustomInputFilesTestFixture()
                : base(ApplicationWithCustomInputFilesTest.ApplicationName)
            {
            }
        }
    }
}
