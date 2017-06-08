// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace FunctionalTests
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

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task ApplicationWithCustomInputFiles_Works(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                var expectedText = "Hello Index!";

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    deployment.DeploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                Assert.Equal(expectedText, response.Trim());
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task MvcRazorFilesToCompile_OverridesTheFilesToBeCompiled(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                var expectedViews = new[]
            {
                "/Views/Home/About.cshtml",
                "/Views/Home/Index.cshtml",
            };

                // Act
                var response2 = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/GetPrecompiledResourceNames",
                    Fixture.Logger);

                // Assert
                var actual = response2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedViews, actual);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task MvcRazorFilesToCompile_SpecificallyDoesNotPublishFilesToBeCompiled(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                var viewsNotPublished = new[]
                {
                    "Index.cshtml",
                    "About.cshtml",
                };

                var viewsPublished = new[]
                {
                    "NotIncluded.cshtml",
                };
                var viewsDirectory = Path.Combine(deployment.DeploymentResult.ContentRoot, "Views", "Home");

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
