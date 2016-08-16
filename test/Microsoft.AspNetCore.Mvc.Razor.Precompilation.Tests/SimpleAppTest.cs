// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class SimpleAppTest : IClassFixture<SimpleAppTest.SimpleAppTestFixture>
    {
        public SimpleAppTest(SimpleAppTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Theory]
        [InlineData(RuntimeFlavor.Clr)]
        [InlineData(RuntimeFlavor.CoreClr)]
        public async Task Precompilation_WorksForSimpleApps(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(deploymentResult.ApplicationBaseUri)
                };

                // Act
                var response = await httpClient.GetStringAsync("");

                // Assert
                TestEmbeddedResource.AssertContent("SimpleAppTest.Home.Index.txt", response);
            }
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
