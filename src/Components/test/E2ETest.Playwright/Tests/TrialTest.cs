// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Tests;

public class TrialTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public TrialTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void SomeTest()
    {
        using var httpClient = new HttpClient { BaseAddress = _serverFixture.RootUri };
        var response = httpClient.GetStringAsync(ServerPathBase).Result;
        Assert.StartsWith("<!DOCTYPE html>", response);

        //Navigate(ServerPathBase, noReload: false);
    }
}
