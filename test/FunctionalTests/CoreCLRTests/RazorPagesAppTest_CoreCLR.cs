// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class RazorPagesAppTest_CoreCLR :
        LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<RazorPagesApp.Startup>>
    {
        public RazorPagesAppTest_CoreCLR(
            CoreCLRApplicationTestFixture<RazorPagesApp.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksForIndexPage_UsingFolderName()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/",
                loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
            }
        }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksForIndexPage_UsingFileName()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/Index",
                loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Index.txt", response);
            }
        }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksForPageWithModel()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/PageWithModel?person=Dan",
                loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.PageWithModel.txt", response);
            }
        }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksForPageWithRoute()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/PageWithRoute/Dan",
                loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.PageWithRoute.txt", response);
            }
        }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksForPageInNestedFolder()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "/Nested1/Nested2/PageWithTagHelper",
                loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("RazorPages.Nested1.Nested2.PageWithTagHelper.txt", response);
            }
        }

        [Fact(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/287")]
        public async Task Precompilation_WorksWithPageConventions()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await RetryHelper.RetryRequest(
                    () => deployment.HttpClient.GetAsync("/Auth/Index"),
                    loggerFactory.CreateLogger(Fixture.ApplicationName),
                    retryCount: 5);

                // Assert
                Assert.Equal("/Login?ReturnUrl=%2FAuth%2FIndex", response.RequestMessage.RequestUri.PathAndQuery);
            }
        }
    }
}
