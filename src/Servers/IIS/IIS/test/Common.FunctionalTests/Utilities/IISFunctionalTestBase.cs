// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities
{
    public class IISFunctionalTestBase : FunctionalTestsBase
    {
        protected static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);
        protected PublishedSitesFixture Fixture { get; set; }

        public IISFunctionalTestBase(PublishedSitesFixture fixture, ITestOutputHelper output = null) : base(output)
        {
            Fixture = fixture;
        }

        public async Task<IISDeploymentResult> DeployApp(HostingModel hostingModel = HostingModel.InProcess)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: hostingModel);

            return await DeployAsync(deploymentParameters);
        }

        public void AddAppOffline(string appPath, string content = "The app is offline.")
        {
            File.WriteAllText(Path.Combine(appPath, "app_offline.htm"), content);
        }

        public void RemoveAppOffline(string appPath)
        {
            RetryHelper.RetryOperation(
                () => File.Delete(Path.Combine(appPath, "app_offline.htm")),
                e => Logger.LogError($"Failed to remove app_offline : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: RetryDelay.Milliseconds);
        }

        public async Task AssertAppOffline(IISDeploymentResult deploymentResult, string expectedResponse = "The app is offline.")
        {
            var response = await deploymentResult.HttpClient.RetryRequestAsync("HelloWorld", r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
            Assert.Equal(expectedResponse, await response.Content.ReadAsStringAsync());
        }

        public async Task<IISDeploymentResult> AssertStarts(HostingModel hostingModel)
        {
            var deploymentResult = await DeployApp(hostingModel);

            await AssertRunning(deploymentResult);

            return deploymentResult;
        }

        public static async Task AssertRunning(IISDeploymentResult deploymentResult)
        {
            var response = await deploymentResult.HttpClient.RetryRequestAsync("HelloWorld", r => r.IsSuccessStatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);
        }

        public void DeletePublishOutput(IISDeploymentResult deploymentResult)
        {
            foreach (var file in Directory.GetFiles(deploymentResult.ContentRoot, "*", SearchOption.AllDirectories))
            {
                // Out of process module dll is allowed to be locked
                var name = Path.GetFileName(file);
                if (name == "aspnetcore.dll" || name == "aspnetcorev2.dll" || name == "aspnetcorev2_outofprocess.dll")
                {
                    continue;
                }
                File.Delete(file);
            }
        }
    }
}
