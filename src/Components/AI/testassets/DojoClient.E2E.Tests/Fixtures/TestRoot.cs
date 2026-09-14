// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Fixtures;

[TestClass]
public static class TestRoot
{
    private static readonly Lazy<Task<ServerFactory<E2ETestAssembly>>> Servers = new(CreateServersAsync);

    internal static float ExpectTimeout { get; private set; }

    [AssemblyInitialize]
    public static void Init(TestContext context)
    {
        ExpectTimeout = context.Properties.TryGetValue("DojoExpectTimeout", out var configured) && configured is not null
            ? Convert.ToSingle(configured, System.Globalization.CultureInfo.InvariantCulture)
            : 30_000;
        if (!float.IsFinite(ExpectTimeout) || ExpectTimeout <= 0)
        {
            throw new InvalidOperationException("DojoExpectTimeout must be a positive number of milliseconds.");
        }

        Assertions.SetDefaultExpectTimeout(ExpectTimeout);
    }

    internal static Task<ServerFactory<E2ETestAssembly>> GetServersAsync() => Servers.Value;

    private static async Task<ServerFactory<E2ETestAssembly>> CreateServersAsync()
    {
        var servers = new ServerFactory<E2ETestAssembly>();
        await servers.InitializeAsync();

        return servers;
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (Servers.IsValueCreated)
        {
            var servers = await Servers.Value;
            await servers.DisposeAsync();
        }
    }
}
