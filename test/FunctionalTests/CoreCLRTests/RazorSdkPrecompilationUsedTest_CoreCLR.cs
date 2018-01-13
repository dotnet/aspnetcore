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
    public class RazorSdkPrecompilationUsedTest_CoreCLR : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithRazorSdkPrecompilationUsed.Startup>>
    {
        public RazorSdkPrecompilationUsedTest_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationWithRazorSdkPrecompilationUsed.Startup> fixture,
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

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    deployment.ApplicationBaseUri,
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                Assert.True(File.Exists(Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkPrecompilationUsed.PrecompiledViews.dll")));
                Assert.False(File.Exists(Path.Combine(deployment.ContentRoot, "ApplicationWithRazorSdkPrecompilationUsed.Views.dll")));
                TestEmbeddedResource.AssertContent("ApplicationWithRazorSdkPrecompilationUsed.Home.Index.txt", response);
            }
        }
    }
}
