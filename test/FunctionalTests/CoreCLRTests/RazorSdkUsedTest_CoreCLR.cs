// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    // Tests that cover cases where both Razor SDK and MvcPrecompilation are installed. This is the default in 2.1
    public class RazorSdkUsedTest_CoreCLR : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithRazorSdkUsed.Startup>>
    {
        public RazorSdkUsedTest_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationWithRazorSdkUsed.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Publish_UsesRazorSDK()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);
                var expectedViewLocation = Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkUsed.Views.dll");
                var expectedPrecompiledViewsLocation = Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkUsed.PrecompiledViews.dll");

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    deployment.ApplicationBaseUri,
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                Assert.False(File.Exists(expectedPrecompiledViewsLocation), $"{expectedPrecompiledViewsLocation} existed, but shouldn't have.");
                Assert.True(File.Exists(expectedViewLocation), $"{expectedViewLocation} didn't exist.");
                TestEmbeddedResource.AssertContent("ApplicationWithRazorSdkUsed.Home.Index.txt", response);
            }
        }
    }
}
