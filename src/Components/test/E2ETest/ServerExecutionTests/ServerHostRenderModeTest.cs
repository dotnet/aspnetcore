// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.Components.E2ETests.Tests;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;

public class ServerInteractiveHostRenderModeTest : InteractiveHostRendermodeTest
{
    public ServerInteractiveHostRenderModeTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output) : base(browserFixture, serverFixture.WithServerExecution(), output)
    {
    }

    public override string ExpectedValue => "Interactive Server";
}
