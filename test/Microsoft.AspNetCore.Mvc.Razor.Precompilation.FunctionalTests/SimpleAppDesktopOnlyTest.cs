// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class SimpleAppDesktopOnlyTest : IClassFixture<SimpleAppDesktopOnlyTest.SimpleAppDesktopOnlyTestFixture>
    {
        public SimpleAppDesktopOnlyTest(SimpleAppDesktopOnlyTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        [OSSkipConditionAttribute(OperatingSystems.Linux)]
        [OSSkipConditionAttribute(OperatingSystems.MacOSX)]
        public async Task Precompilation_WorksForSimpleApps()
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(RuntimeFlavor.Clr))
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("SimpleAppDesktopOnly.Home.Index.txt", response);
            }
        }

        public class SimpleAppDesktopOnlyTestFixture : ApplicationTestFixture
        {
            public SimpleAppDesktopOnlyTestFixture()
                : base("SimpleAppDesktopOnly")
            {
            }
        }
    }
}
