// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.Microbenchmarks;

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
        var deploymentParameters = new DeploymentParameters(Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"), "IIS/test/testassets/InProcessWebSite"),
            ServerType.IISExpress,
            RuntimeFlavor.CoreClr,
            RuntimeArchitecture.x64)
        {
#pragma warning restore 0618
            ServerConfigTemplateContent = File.ReadAllText("IIS.config"),
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
