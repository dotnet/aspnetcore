// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.OutOfProcess
{
    [Collection(PublishedSitesCollection.Name)]
    public class HelloWorldTests : IISFunctionalTestBase
    {
        public HelloWorldTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllApplicationTypes();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HelloWorld(TestVariant variant)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.ServerConfigActionList.Add(
                (element, _) =>
                {
                    element
                        .RequiredElement("system.webServer")
                        .RequiredElement("security")
                        .RequiredElement("authentication")
                        .Element("windowsAuthentication")
                        ?.SetAttributeValue("enabled", "false");
                });

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/HelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);

            response = await deploymentResult.HttpClient.GetAsync("/Path/%3F%3F?query");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("/??", responseText);

            response = await deploymentResult.HttpClient.GetAsync("/Query/%3FPath?query?");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("?query?", responseText);

            response = await deploymentResult.HttpClient.GetAsync("/BodyLimit");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("null", responseText);

            response = await deploymentResult.HttpClient.GetAsync("/Auth");
            responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("null", responseText);

            // Trailing slash
            Assert.StartsWith(deploymentResult.ContentRoot, await deploymentResult.HttpClient.GetStringAsync("/ContentRootPath"));
            Assert.Equal(deploymentResult.ContentRoot + "\\wwwroot", await deploymentResult.HttpClient.GetStringAsync("/WebRootPath"));
            var expectedDll = "aspnetcorev2.dll";
            Assert.Contains(deploymentResult.HostProcess.Modules.OfType<ProcessModule>(), m => m.FileName.Contains(expectedDll));

            if (DeployerSelector.HasNewHandler && variant.HostingModel == HostingModel.InProcess)
            {
                Assert.Equal(deploymentResult.ContentRoot, await deploymentResult.HttpClient.GetStringAsync("/CurrentDirectory"));
                Assert.Equal(Path.GetDirectoryName(deploymentResult.HostProcess.MainModule.FileName), await deploymentResult.HttpClient.GetStringAsync("/DllDirectory"));
            }
        }
    }
}
