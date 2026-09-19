// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlatformTestApp.E2E.Tests.Fixtures;

[TestClass]
public sealed class TestRoot
{
    public static ServerFactory<TestRoot> Servers { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        Servers = new ServerFactory<TestRoot>();
        await Servers.InitializeAsync();
    }

    [AssemblyCleanup]
    public static Task Cleanup()
    {
        return Servers.DisposeAsync().AsTask();
    }
}
