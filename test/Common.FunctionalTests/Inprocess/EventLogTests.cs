using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class EventLogTests : IISFunctionalTestBase
    {
        [ConditionalFact]
        public async Task CheckStartupEventLogMessage()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await DeployAsync(deploymentParameters);
            await Helpers.AssertStarts(deploymentResult);

            StopServer();

            EventLogHelpers.VerifyEventLogEvent(TestSink, "Application '.+' started the coreclr in-process successfully.");
        }
    }
}
