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
    public class SimpleAppWithAssemblyRenameTest_Desktop :
        LoggedTest, IClassFixture<SimpleAppWithAssemblyRenameTest_Desktop.TestFixture>
    {
        public SimpleAppWithAssemblyRenameTest_Desktop(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        public async Task Precompilation_WorksForSimpleApps()
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
                TestEmbeddedResource.AssertContent("SimpleAppWithAssemblyRenameTest.Home.Index.txt", response);
            }
        }

        public class TestFixture : DesktopApplicationTestFixture<SimpleAppWithAssemblyRename.Startup>
        {
            public TestFixture()
                : base(
                    typeof(SimpleAppWithAssemblyRename.Startup).Assembly.GetName().Name,
                    ApplicationPaths.GetTestAppDirectory(nameof(SimpleAppWithAssemblyRename)))
            {
            }
        }
    }
}
