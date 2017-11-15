// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class ApplicationConsumingPrecompiledViews_CoreCLR
        : LoggedTest, IClassFixture<CoreCLRApplicationTestFixture<ApplicationUsingPrecompiledViewClassLibrary.Startup>>
    {
        public ApplicationConsumingPrecompiledViews_CoreCLR(
            CoreCLRApplicationTestFixture<ApplicationUsingPrecompiledViewClassLibrary.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task ConsumingClassLibrariesWithPrecompiledViewsWork()
        {
            // Arrange
            using (StartLog(out var loggerFactory))
            {
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Manage/Home",
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("ApplicationConsumingPrecompiledViews.Manage.Home.Index.txt", response);
            }
        }
    }
}