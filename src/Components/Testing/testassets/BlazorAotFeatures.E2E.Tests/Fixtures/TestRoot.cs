// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorAotFeatures.E2E.Tests.Fixtures;

[TestClass]
public static class TestRoot
{
    public static ServerFactory<E2ETestAssembly> Servers { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        Servers = new ServerFactory<E2ETestAssembly>();
        await Servers.InitializeAsync();
    }

    [AssemblyCleanup]
    public static Task CleanupAsync() => Servers.DisposeAsync().AsTask();
}
