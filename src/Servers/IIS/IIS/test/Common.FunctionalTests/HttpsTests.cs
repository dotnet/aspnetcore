// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class HttpsTests : IISFunctionalTestBase
    {
        public HttpsTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp30)
                .WithAllApplicationTypes()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task HttpsHelloWorld(TestVariant variant)
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var client = CreateNonValidatingClient(deploymentResult);
            var response = await client.GetAsync("HttpsHelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();
            if (variant.HostingModel == HostingModel.OutOfProcess)
            {
                Assert.Equal("Scheme:https; Original:http", responseText);
            }
            else
            {
                Assert.Equal("Scheme:https; Original:", responseText);
            }

            if (DeployerSelector.HasNewHandler &&
                DeployerSelector.HasNewShim)
            {
                // We expect ServerAddress to be set for InProcess and HTTPS_PORT for OutOfProcess
                if (variant.HostingModel == HostingModel.InProcess)
                {
                    Assert.Equal(deploymentParameters.ApplicationBaseUriHint, await client.GetStringAsync("/ServerAddresses"));
                }
                else
                {
                    Assert.Equal(port.ToString(), await client.GetStringAsync("/HTTPS_PORT"));
                }
            }
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresNewShim]
        public async Task ServerAddressesIncludesBaseAddress()
        {
            var appName = "\u041C\u043E\u0451\u041F\u0440\u0438\u043B\u043E\u0436\u0435\u043D\u0438\u0435";

            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();
            deploymentParameters.AddServerConfigAction(
                (element, root) => {
                    element.Descendants("site").Single().Element("application").SetAttributeValue("path", "/" + appName);
                    Helpers.CreateEmptyApplication(element, root);
                });

            var deploymentResult = await DeployAsync(deploymentParameters);
            var client = CreateNonValidatingClient(deploymentResult);

            Assert.Equal(deploymentParameters.ApplicationBaseUriHint + appName, await client.GetStringAsync($"/{appName}/ServerAddresses"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresNewShim]
        public async Task HttpsPortCanBeOverriden()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

            deploymentParameters.AddServerConfigAction(
                element => {
                    element.Descendants("bindings")
                        .Single()
                        .GetOrAdd("binding", "protocol", "https")
                        .SetAttributeValue("bindingInformation", $":{TestPortHelper.GetNextSSLPort()}:localhost");
                });

            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_HTTPS_PORT"] = "123";

            var deploymentResult = await DeployAsync(deploymentParameters);
            var client = CreateNonValidatingClient(deploymentResult);

            Assert.Equal("123", await client.GetStringAsync("/HTTPS_PORT"));
        }

        [ConditionalFact]
        [RequiresNewHandler]
        [RequiresNewShim]
        public async Task MultipleHttpsPortsProduceNoEnvVar()
        {
            var sslPort = GetNextSSLPort();
            var anotherSslPort = GetNextSSLPort(sslPort);

            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

            deploymentParameters.AddServerConfigAction(
                element => {
                    element.Descendants("bindings")
                        .Single()
                        .Add(
                            new XElement("binding",
                                new XAttribute("protocol", "https"),
                                new XAttribute("bindingInformation",  $":{sslPort}:localhost")),
                            new XElement("binding",
                                new XAttribute("protocol", "https"),
                                new XAttribute("bindingInformation",  $":{anotherSslPort}:localhost")));
                });

            var deploymentResult = await DeployAsync(deploymentParameters);
            var client = CreateNonValidatingClient(deploymentResult);

            Assert.Equal("NOVALUE", await client.GetStringAsync("/HTTPS_PORT"));
        }

        private static HttpClient CreateNonValidatingClient(IISDeploymentResult deploymentResult)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            return deploymentResult.CreateClient(handler);
        }

        public static int GetNextSSLPort(int avoid = 0)
        {
            var next = 44300;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                while (true)
                {
                    try
                    {
                        var port = next++;
                        if (port == avoid)
                        {
                            continue;
                        }
                        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                        return port;
                    }
                    catch (SocketException)
                    {
                        // Retry unless exhausted
                        if (next > 44399)
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
