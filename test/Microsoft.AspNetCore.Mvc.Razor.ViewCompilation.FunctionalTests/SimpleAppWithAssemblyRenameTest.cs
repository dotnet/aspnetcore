// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppWithAssemblyRenameTest : IClassFixture<SimpleAppWithAssemblyRenameTest.TestFixture>
    {
        public SimpleAppWithAssemblyRenameTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForSimpleApps(RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.CreateDeployment(flavor);

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                Fixture.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("SimpleAppWithAssemblyRenameTest.Home.Index.txt", response);
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleAppWithAssemblyRename")
            {
            }

            public override DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor)
            {
                var parameters = base.GetDeploymentParameters(flavor);
                parameters.ApplicationName = "NewAssemblyName";
                return parameters;
            }
        }
    }
}
