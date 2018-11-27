// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class SimpleAppWithAssemblyRenameTest_CoreCLR :
        LoggedTest, IClassFixture<SimpleAppWithAssemblyRenameTest_CoreCLR.TestFixture>
    {
        public SimpleAppWithAssemblyRenameTest_CoreCLR(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
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

        public class TestFixture : CoreCLRApplicationTestFixture<SimpleAppWithAssemblyRename.Startup>
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
