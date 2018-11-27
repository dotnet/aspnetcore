// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class ApplicationWithConfigureMvcTest_CoreCLR
        : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithConfigureStartup.Startup>>
    {
        public ApplicationWithConfigureMvcTest_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationWithConfigureStartup.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_RunsConfiguredCompilationCallbacks()
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
                TestEmbeddedResource.AssertContent("ApplicationWithConfigureMvc.Home.Index.txt", response);
            }
        }

        [Fact]
        public async Task Precompilation_UsesConfiguredParseOptions()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/ViewWithPreprocessor",
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent(
                    "ApplicationWithConfigureMvc.Home.ViewWithPreprocessor.txt",
                    response);
            }
        }
    }
}
