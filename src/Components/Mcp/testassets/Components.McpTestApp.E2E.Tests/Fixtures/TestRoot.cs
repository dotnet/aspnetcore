// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Components.McpTestApp.E2E.Tests.Fixtures;

[TestClass]
public static class TestRoot
{
    public static ServerFactory<McpTestAssembly> Servers { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Initialize(TestContext _)
    {
        Servers = new ServerFactory<McpTestAssembly>();
        await Servers.InitializeAsync();
    }

    public sealed class McpTestAssembly;

    [AssemblyCleanup]
    public static Task Cleanup() => Servers.DisposeAsync().AsTask();
}
