// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    // Tests that cover cases where both Razor SDK and MvcPrecompilation are installed. This is the default in 2.1
    public class RazorSdkNeitherUsedTest_CoreCLR : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithRazorSdkNeitherUsed.Startup>>
    {
        public RazorSdkNeitherUsedTest_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationWithRazorSdkNeitherUsed.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Publish_HasNoPrecompilation()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await RetryHelper.RetryRequest(
                    () => deployment.HttpClient.GetAsync(deployment.ApplicationBaseUri),
                    loggerFactory.CreateLogger(Fixture.ApplicationName),
                    retryCount: 5);

                // Assert
                Assert.False(File.Exists(Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkNeitherUsed.PrecompiledViews.dll")));
                Assert.False(File.Exists(Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkNeitherUsed.Views.dll")));
            }
        }
    }
}
