// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppTest : IClassFixture<SimpleAppTest.SimpleAppTestFixture>
    {
        public SimpleAppTest(SimpleAppTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_WorksForSimpleApps()
        {
            // Arrange
            var deploymentResult = Fixture.CreateDeployment();

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                deploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("SimpleAppTest.Home.Index.txt", response);
        }

        public class SimpleAppTestFixture : ApplicationTestFixture
        {
            public SimpleAppTestFixture()
                : base("SimpleApp")
            {
            }
        }
    }
}
