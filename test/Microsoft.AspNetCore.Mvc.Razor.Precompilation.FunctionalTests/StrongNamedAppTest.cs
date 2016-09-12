// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class StrongNamedAppTest : IClassFixture<StrongNamedAppTest.StrongNamedAppFixture>
    {
        public StrongNamedAppTest(StrongNamedAppFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task PrecompiledAssembliesUseSameStrongNameAsApplication(RuntimeFlavor flavor)
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
                TestEmbeddedResource.AssertContent("StrongNamedApp.Home.Index.txt", response);
            }
        }

        public class StrongNamedAppFixture : ApplicationTestFixture
        {
            public StrongNamedAppFixture()
                : base("StrongNamedApp")
            {
            }
        }
    }
}
