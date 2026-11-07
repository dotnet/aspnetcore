// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerInteropCancellationTest(
    BrowserFixture browserFixture,
    ToggleExecutionModeServerFixture<Program> serverFixture,
    ITestOutputHelper output) : InteropCancellationTest(browserFixture, serverFixture.WithServerExecution(), output);
