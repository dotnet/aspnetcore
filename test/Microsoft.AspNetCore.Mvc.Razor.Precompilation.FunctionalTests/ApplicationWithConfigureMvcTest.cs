// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class ApplicationWithConfigureMvcTest
        : IClassFixture<ApplicationWithConfigureMvcTest.ApplicationWithConfigureMvcFixture>
    {
        public ApplicationWithConfigureMvcTest(ApplicationWithConfigureMvcFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static IEnumerable<object[]> SupportedFlavorsTheoryData
        {
            get
            {
                return RuntimeFlavors.SupportedFlavors.Select(f => new object[] { f });
            }
        }

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_RunsConfiguredCompilationCallbacks(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("ApplicationWithConfigureMvc.Home.Index.txt", response);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_UsesConfiguredParseOptions(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri + "Home/ViewWithPreprocessor",
                    Fixture.Logger);

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
