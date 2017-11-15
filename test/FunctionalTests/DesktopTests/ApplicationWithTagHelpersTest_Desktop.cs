// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public class ApplicationWithTagHelpersTest_Desktop :
        LoggedTest, IClassFixture<DesktopApplicationTestFixture<ApplicationWithTagHelpers.Startup>>
    {
        public ApplicationWithTagHelpersTest_Desktop(
            DesktopApplicationTestFixture<ApplicationWithTagHelpers.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        public async Task Precompilation_WorksForViewsThatUseTagHelpersFromProjectReferences()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/ClassLibraryTagHelper",
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent($"ApplicationWithTagHelpers.Home.ClassLibraryTagHelper.txt", response);
            }
        }

        [ConditionalFact]
        public async Task Precompilation_WorksForViewsThatUseTagHelpersFromCurrentProject()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    "Home/LocalTagHelper",
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent($"ApplicationWithTagHelpers.Home.LocalTagHelper.txt", response);
            }
        }
    }
}
