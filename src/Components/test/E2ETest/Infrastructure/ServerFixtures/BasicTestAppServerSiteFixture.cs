// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class BasicTestAppServerSiteFixture<TStartup> : AspNetSiteServerFixture where TStartup : class
{
    public readonly bool TestTrimmedOrMultithreadingApps = typeof(BasicTestAppServerSiteFixture<>).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedOrMultithreadingApps")
        .Value == "true";

    public BasicTestAppServerSiteFixture()
    {
        ApplicationAssembly = typeof(TStartup).Assembly;
        BuildWebHostMethod = TestServer.Program.BuildWebHost<TStartup>;
    }
}
