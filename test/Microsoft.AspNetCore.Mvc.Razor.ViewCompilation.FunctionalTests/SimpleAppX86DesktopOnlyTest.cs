// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppX86DesktopOnlyTest : IClassFixture<SimpleAppX86DesktopOnlyTest.SimpleAppX86DesktopOnlyFixture>
    {
        public SimpleAppX86DesktopOnlyTest(SimpleAppX86DesktopOnlyFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_WorksForSimpleApps()
        {
            // Arrange
            Fixture.CreateDeployment(RuntimeFlavor.Clr);

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("SimpleAppX86DesktopOnly.Home.Index.txt", response);
        }

        public class SimpleAppX86DesktopOnlyFixture : ApplicationTestFixture
        {
            public SimpleAppX86DesktopOnlyFixture()
                : base("SimpleAppX86DesktopOnly")
            {
            }
        }
    }
}