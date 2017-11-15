// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
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
    public class ViewCompilationOptions_Desktop_ScenarioRefAssembliesDoNotGetPublished :
        LoggedTest, IClassFixture<ViewCompilationOptions_Desktop_ScenarioRefAssembliesDoNotGetPublished.TestFixture>
    {
        public ViewCompilationOptions_Desktop_ScenarioRefAssembliesDoNotGetPublished(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [ConditionalFact]
        public async Task PublishingWithOption_AllowsPublishingRefAssemblies()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Act & Assert
                Assert.True(Directory.Exists(Path.Combine(deployment.ContentRoot, "refs")));
            }
        }

        public class TestFixture : DesktopApplicationTestFixture<SimpleApp.Startup>
        {
            public TestFixture()
            {
                PublishOnly = true;
            }

            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.PublishEnvironmentVariables.Add(
                    new KeyValuePair<string, string>("MvcRazorExcludeRefAssembliesFromPublish", "false"));

                return deploymentParameters;
            }
        }
    }
}
