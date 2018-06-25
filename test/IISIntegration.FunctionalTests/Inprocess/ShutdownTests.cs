// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using IISIntegration.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [SkipIfIISExpressSchemaMissingInProcess]
    public class ShutdownTests : IISFunctionalTestBase
    {

        public ShutdownTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public async Task ServerShutsDownWhenMainExits()
        {
            var parameters = Helpers.GetBaseDeploymentParameters();
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/Shutdown");
            Assert.True(result.DeploymentResult.HostShutdownToken.WaitHandle.WaitOne(Helpers.DefaultTimeout));
        }
    }
}
