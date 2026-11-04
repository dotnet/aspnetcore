// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerJsInitializersTest : JsInitializersTest
{
    public ServerJsInitializersTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture.WithServerExecution(), output)
    {
    }

    protected override string[] GetExpectedCallbacks()
    {
        return ["classic-before-start",
            "classic-after-started",
            "classic-and-modern-before-server-start",
            "classic-and-modern-after-server-started",
            "classic-and-modern-circuit-opened",
            "modern-before-server-start",
            "modern-after-server-started",
            "modern-circuit-opened",
        ];
    }
}
