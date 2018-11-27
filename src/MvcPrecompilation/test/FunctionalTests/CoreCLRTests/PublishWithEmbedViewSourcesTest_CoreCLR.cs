// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class PublishWithEmbedViewSourcesTest_CoreCLR
        : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<PublishWithEmbedViewSources.Startup>>
    {
        public PublishWithEmbedViewSourcesTest_CoreCLR(
            CoreCLRApplicationTestFixture<PublishWithEmbedViewSources.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_CanEmbedViewSourcesAsResources()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var logger = loggerFactory.CreateLogger(Fixture.ApplicationName);
                var expectedViews = new[]
                {
                    "/Areas/TestArea/Views/Home/Index.cshtml",
                    "/Views/Home/About.cshtml",
                    "/Views/Home/Index.cshtml",
                };
                var expectedText = "Hello Index!";

                // Act - 1
                var response1 = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/Index",
                    logger);

                // Assert - 1
                Assert.Equal(expectedText, response1.Trim());

                // Act - 2
                var response2 = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/GetPrecompiledResourceNames",
                    logger);

                // Assert - 2
                var actual = response2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedViews, actual);
            }
        }
    }
}
