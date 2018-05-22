// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public class ApplicationWithTagHelpersTest_Desktop :
        LoggedTest, IClassFixture<ApplicationWithTagHelpersTest_Desktop.ApplicationWithTagHelpersTestFixture>
    {
        public ApplicationWithTagHelpersTest_Desktop(
            ApplicationWithTagHelpersTestFixture fixture,
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

        public class ApplicationWithTagHelpersTestFixture : DesktopApplicationTestFixture<ApplicationWithTagHelpers.Startup>
        {
            protected override Task<DeploymentResult> CreateDeploymentAsyncCore(ILoggerFactory loggerFactory)
            {
                CopyDirectory(
                    new DirectoryInfo(Path.Combine(ApplicationPath, "..", "ClassLibraryTagHelper")),
                    new DirectoryInfo(Path.Combine(WorkingDirectory, "ClassLibraryTagHelper")));

                return base.CreateDeploymentAsyncCore(loggerFactory);
            }
        }
    }
}
