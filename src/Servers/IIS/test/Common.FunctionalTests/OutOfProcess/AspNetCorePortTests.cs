// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class AspNetCorePortTests : IISFunctionalTestBase
    {
        // Port range allowed by ANCM config
        private const int _minPort = 1025;
        private const int _maxPort = 48000;

        private static readonly Random _random = new Random();

        private readonly PublishedSitesFixture _fixture;

        public AspNetCorePortTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp30)
                .WithApplicationTypes(ApplicationType.Portable)
                .WithAllAncmVersions();

        public static IEnumerable<object[]> InvalidTestVariants
            => from v in TestVariants.Select(v => v.Single())
               from s in new string[] { (_minPort - 1).ToString(), (_maxPort + 1).ToString(), "noninteger" }
               select new object[] { v, s };

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task EnvVarInWebConfig_Valid(TestVariant variant)
        {
            // Must publish to set env vars in web.config
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            var port = GetUnusedRandomPort();
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_PORT"] = port.ToString();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var responseText = await deploymentResult.HttpClient.GetStringAsync("/ServerAddresses");

            Assert.Equal(port, new Uri(responseText).Port);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task EnvVarInWebConfig_Empty(TestVariant variant)
        {
            // Must publish to set env vars in web.config
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_PORT"] = string.Empty;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var responseText = await deploymentResult.HttpClient.GetStringAsync("/ServerAddresses");

            // If env var is empty, ANCM should assign a random port (same as no env var)
            Assert.InRange(new Uri(responseText).Port, _minPort, _maxPort);
        }

        [ConditionalTheory]
        [MemberData(nameof(InvalidTestVariants))]
        public async Task EnvVarInWebConfig_Invalid(TestVariant variant, string port)
        {
            // Must publish to set env vars in web.config
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.WebConfigBasedEnvironmentVariables["ASPNETCORE_PORT"] = port;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/ServerAddresses");

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        private static int GetUnusedRandomPort()
        {
            // Large number of retries to prevent test failures due to port collisions, but not infinite
            // to prevent infinite loop in case Bind() fails repeatedly for some other reason.
            const int retries = 100;

            List<Exception> exceptions = null;

            for (var i = 0; i < retries; i++)
            {
                var port = _random.Next(_minPort, _maxPort);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                        return port;
                    }
                    catch (Exception e)
                    {
                        // Bind failed, most likely because port is in use.  Save exception and retry.
                        if (exceptions == null)
                        {
                            exceptions = new List<Exception>(retries);
                        }
                        exceptions.Add(e);
                    }
                }
            }

            throw new AggregateException($"Unable to find unused random port after {retries} retries.", exceptions);
        }
    }
}
