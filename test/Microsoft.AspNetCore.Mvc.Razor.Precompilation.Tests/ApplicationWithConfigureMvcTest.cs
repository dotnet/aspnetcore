// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Tests
{
    public class ApplicationWithConfigureMvcTest
        : IClassFixture<ApplicationWithConfigureMvcTest.ApplicationWithConfigureMvcFixture>
    {
        public ApplicationWithConfigureMvcTest(ApplicationWithConfigureMvcFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static IEnumerable<object[]> ApplicationWithTagHelpersData
        {
            get
            {
                var runtimeFlavors = new[]
                {
                    RuntimeFlavor.Clr,
                    RuntimeFlavor.CoreClr,
                };

                var urls = new[]
                {
                    "Index",
                    "ViewWithPreprocessor",
                };

                return Enumerable.Zip(urls, runtimeFlavors, (a, b) => new object[] { a, b });
            }
        }

        [Theory]
        [InlineData(RuntimeFlavor.Clr)]
        [InlineData(RuntimeFlavor.CoreClr)]
        public async Task Precompilation_RunsConfiguredCompilationCallbacks(RuntimeFlavor flavor)
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
                TestEmbeddedResource.AssertContent("ApplicationWithConfigureMvc.Home.Index.txt", response);
            }
        }

        [Theory]
        [InlineData(RuntimeFlavor.Clr)]
        [InlineData(RuntimeFlavor.CoreClr)]
        public async Task Precompilation_UsesConfiguredParseOptions(RuntimeFlavor flavor)
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
                var response = await httpClient.GetStringAsync("Home/ViewWithPreprocessor");

                // Assert
                TestEmbeddedResource.AssertContent(
                    "ApplicationWithConfigureMvc.Home.ViewWithPreprocessor.txt",
                    response);
            }
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
