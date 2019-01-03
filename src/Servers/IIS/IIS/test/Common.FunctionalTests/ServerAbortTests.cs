// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public abstract class ServerAbortTests: FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        [Collection(IISTestSiteCollection.Name)]
        public class InProc: ServerAbortTests
        {
            public InProc(IISTestSiteFixture fixture) : base(fixture) { }
        }

        [Collection(OutOfProcessTestSiteCollection.Name)]
        public class OutOfProcess: ServerAbortTests
        {
            public OutOfProcess(OutOfProcessTestSiteFixture fixture) : base(fixture) { }
        }

        [Collection(OutOfProcessV1TestSiteCollection.Name)]
        public class OutOfProcessV1: ServerAbortTests
        {
            public OutOfProcessV1(OutOfProcessV1TestSiteFixture fixture) : base(fixture) { }
        }

        protected ServerAbortTests(IISTestSiteFixture fixture) : base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ClosesConnectionOnServerAbort()
        {
            try
            {
                var response = await _fixture.Client.GetAsync("/Abort").DefaultTimeout();

                // 502 is expected for outofproc but not for inproc
                if (_fixture.DeploymentResult.DeploymentParameters.HostingModel == HostingModel.OutOfProcess)
                {
                    Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
                    // 0x80072f78 ERROR_HTTP_INVALID_SERVER_RESPONSE The server returned an invalid or unrecognized response
                    Assert.Contains("0x80072f78", await response.Content.ReadAsStringAsync());
                }
                else
                {
                    Assert.True(false, "Should not reach here");
                }
            }
            catch (HttpRequestException)
            {
                // Connection reset is expected both for outofproc and inproc
            }
        }
    }
}
