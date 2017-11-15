// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class ApplicationWithCustomInputFilesTest_CoreCLR
        : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithCustomInputFiles.Startup>>
    {
        public ApplicationWithCustomInputFilesTest_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationWithCustomInputFiles.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task ApplicationWithCustomInputFiles_Works()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var expectedText = "Hello Index!";

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    deployment.ApplicationBaseUri,
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                Assert.Equal(expectedText, response.Trim());
            }
        }

        [Fact]
        public async Task MvcRazorFilesToCompile_OverridesTheFilesToBeCompiled()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var expectedViews = new[]
                {
                    "/Views/Home/About.cshtml",
                    "/Views/Home/Index.cshtml",
                };

                // Act
                var response2 = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/GetPrecompiledResourceNames",
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                var actual = response2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedViews, actual);
            }
        }

        [Fact]
        public async Task MvcRazorFilesToCompile_SpecificallyDoesNotPublishFilesToBeCompiled()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var viewsNotPublished = new[]
                {
                    "Index.cshtml",
                    "About.cshtml",
                };

                var viewsPublished = new[]
                {
                    "NotIncluded.cshtml",
                };
                var viewsDirectory = Path.Combine(deployment.ContentRoot, "Views", "Home");

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
    }
}
