// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class StrongNamedAppTest : IClassFixture<StrongNamedAppTest.StrongNamedAppFixture>
    {
        public StrongNamedAppTest(StrongNamedAppFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task PrecompiledAssembliesUseSameStrongNameAsApplication(RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.CreateDeployment(flavor);
            
            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("StrongNamedApp.Home.Index.txt", response);
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
