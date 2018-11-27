// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public class SimpleAppTestWithPlatformx86_Desktop :
        LoggedTest, IClassFixture<DesktopApplicationTestFixture<SimpleApp.Startup>>
    {
        public SimpleAppTestWithPlatformx86_Desktop(
            DesktopApplicationTestFixture<SimpleApp.Startup> fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        public async Task Precompilation_PublishingForPlatform()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                    deployment.ApplicationBaseUri,
                    loggerFactory.CreateLogger(Fixture.ApplicationName));

                // Assert
                TestEmbeddedResource.AssertContent("SimpleAppTest.Home.Index.txt", response);
            }
        }

        public class SimpleAppTestWithPlatformx86_DesktopFixture : DesktopApplicationTestFixture<SimpleApp.Startup>
        {
            protected override DeploymentParameters GetDeploymentParameters()
            {
                var parameters = base.GetDeploymentParameters();
                parameters.AdditionalPublishParameters = "/p:Platform=x86";

                return parameters;
            }
        }
    }
}
