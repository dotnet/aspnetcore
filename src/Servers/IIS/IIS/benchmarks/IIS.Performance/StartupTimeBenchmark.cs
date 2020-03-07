// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.Performance
{
    [AspNetCoreBenchmark(typeof(FirstRequestConfig))]
    public class StartupTimeBenchmark
    {
        private ApplicationDeployer _deployer;
        public HttpClient _client;

        [IterationSetup]
        public void Setup()
        {
// Deployers do not work in distributed environments
// see https://github.com/dotnet/aspnetcore/issues/10268 and https://github.com/dotnet/extensions/issues/1697
#pragma warning disable 0618
            var deploymentParameters = new DeploymentParameters(Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"), "test/testassets/InProcessWebSite"),
                ServerType.IISExpress,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
#pragma warning restore 0618
                ServerConfigTemplateContent = File.ReadAllText("IISExpress.config"),
                SiteName = "HttpTestSite",
                TargetFramework = "netcoreapp2.1",
                ApplicationType = ApplicationType.Portable
            };
            _deployer = ApplicationDeployerFactory.Create(deploymentParameters, NullLoggerFactory.Instance);
            _client = _deployer.DeployAsync().Result.HttpClient;
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _deployer.Dispose();
        }

        [Benchmark]
        public async Task SendFirstRequest()
        {
            var response = await _client.GetAsync("");
        }
    }
}
