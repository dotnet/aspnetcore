// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ApplicationWithConfigureMvcTest
        : IClassFixture<ApplicationWithConfigureMvcTest.ApplicationWithConfigureMvcFixture>
    {
        public ApplicationWithConfigureMvcTest(ApplicationWithConfigureMvcFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_RunsConfiguredCompilationCallbacks()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("ApplicationWithConfigureMvc.Home.Index.txt", response);
        }

        [Fact]
        public async Task Precompilation_UsesConfiguredParseOptions()
        {
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                "Home/ViewWithPreprocessor",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent(
                "ApplicationWithConfigureMvc.Home.ViewWithPreprocessor.txt",
                response);
        }

        public class ApplicationWithConfigureMvcFixture : ApplicationTestFixture
        {
            public ApplicationWithConfigureMvcFixture()
                : base("ApplicationWithConfigureMvc")
            {
            }
        }
    }
}
