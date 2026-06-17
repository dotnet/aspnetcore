// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class AspNetSiteServerFixture : WebHostServerFixture
{
    public delegate IHost BuildWebHost(string[] args);

    public delegate string GetContentRoot(Assembly assembly);

    public Assembly ApplicationAssembly { get; set; }

    public BuildWebHost BuildWebHostMethod { get; set; }

    public Action<IServiceProvider> UpdateHostServices { get; set; }

    public GetContentRoot GetContentRootMethod { get; set; } = DefaultGetContentRoot;

    public AspNetEnvironment Environment { get; set; } = AspNetEnvironment.Production;

    public List<string> AdditionalArguments { get; set; } = new List<string> { "--test-execution-mode", "server" };

    protected override IHost CreateWebHost()
    {
        if (BuildWebHostMethod == null)
        {
            throw new InvalidOperationException(
                $"No value was provided for {nameof(BuildWebHostMethod)}");
        }

        var assembly = ApplicationAssembly ?? BuildWebHostMethod.Method.DeclaringType.Assembly;
        var sampleSitePath = GetContentRootMethod(assembly);

        var host = "127.0.0.1";
        if (E2ETestOptions.Instance.SauceTest)
        {
            host = E2ETestOptions.Instance.Sauce.HostName;
        }

        var result = BuildWebHostMethod(new[]
        {
                "--urls", $"http://{host}:0",
                "--contentroot", sampleSitePath,
                "--environment", Environment.ToString(),
            }.Concat(AdditionalArguments).ToArray());

        UpdateHostServices?.Invoke(result.Services);

        return result;
    }

    private static string DefaultGetContentRoot(Assembly assembly)
        => FindSampleOrTestSitePath(assembly.FullName);
}
