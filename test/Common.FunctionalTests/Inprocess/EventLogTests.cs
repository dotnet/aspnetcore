// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class EventLogTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public EventLogTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task CheckStartupEventLogMessage()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            
            var deploymentResult = await DeployAsync(deploymentParameters);
            await deploymentResult.AssertStarts();

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Application '.+' started the coreclr in-process successfully.");
        }

        [ConditionalFact]
        public async Task CheckShutdownEventLogMessage()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await DeployAsync(deploymentParameters);
            await deploymentResult.AssertStarts();

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(deploymentResult, "Application '.+' has shutdown.");
        }
    }
}
