// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ClientDisconnectStressTests: IISFunctionalTestBase
    {
        public ClientDisconnectStressTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        public async Task ClosesConnectionOnServerAbortOutOfProcess()
        {
            try
            {
                var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

                var deploymentResult = await DeployAsync(deploymentParameters);

                var response = await deploymentResult.HttpClient.GetAsync("/Abort").DefaultTimeout();

                Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
                // 0x80072f78 ERROR_HTTP_INVALID_SERVER_RESPONSE The server returned an invalid or unrecognized response
                Assert.Contains("0x80072f78", await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                // Connection reset is expected
            }
        }

        [ConditionalFact]
        public async Task ClosesConnectionOnServerAbortInProcess()
        {
            try
            {
                var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);

                var deploymentResult = await DeployAsync(deploymentParameters);
                var response = await deploymentResult.HttpClient.GetAsync("/Abort").DefaultTimeout();

                Assert.True(false, "Should not reach here");
            }
            catch (HttpRequestException)
            {
                // Connection reset is expected both for outofproc and inproc
            }
        }

        [ConditionalTheory]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
        [InlineData(HostingModel.InProcess)]
        [InlineData(HostingModel.OutOfProcess)]
        public async Task ClientDisconnectStress(HostingModel hostingModel)
        {
            var site = await StartAsync(Fixture.GetBaseDeploymentParameters(hostingModel));
            var maxRequestSize = 1000;
            var blockSize = 40;
            var random = new Random();
            async Task RunRequests()
            {
                using (var connection = new TestConnection(site.HttpClient.BaseAddress.Port))
                {
                    await connection.Send(
                        "POST /ReadAndFlushEcho HTTP/1.1",
                        $"Content-Length: {maxRequestSize}",
                        "Host: localhost",
                        "Connection: close",
                        "",
                        "");

                    var disconnectAfter = random.Next(maxRequestSize);
                    var data = new byte[blockSize];
                    for (int i = 0; i < disconnectAfter / blockSize; i++)
                    {
                        await connection.Stream.WriteAsync(data);
                    }
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(RunRequests));
            }

            await Task.WhenAll(tasks);

            StopServer();
        }
    }
}
