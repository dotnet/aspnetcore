// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ShutdownTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public ShutdownTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ServerShutsDownWhenMainExits()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);
            try
            {
                await result.HttpClient.GetAsync("/Shutdown");
            }
            catch (HttpRequestException ex) when (ex.InnerException is IOException)
            {
                // Server might close a connection before request completes
            }
            Assert.True(result.HostShutdownToken.WaitHandle.WaitOne(TimeoutExtensions.DefaultTimeout));
        }

        [ConditionalFact]
        public async Task GracefulShutdown_DoesNotCrashProcess()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            parameters.GracefulShutdown = true;
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/HelloWorld");
            StopServer();
            Assert.True(result.HostProcess.ExitCode == 0);
        }

        [ConditionalFact]
        public async Task ForcefulShutdown_DoesrashProcess()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/HelloWorld");
            StopServer();
            Assert.True(result.HostProcess.ExitCode == 1);
        }
    }
}
