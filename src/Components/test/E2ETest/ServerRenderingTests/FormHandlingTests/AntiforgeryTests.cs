// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class AntiforgeryTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public AntiforgeryTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57766")]
    public void CanUseAntiforgeryAfterInitialRender(string target)
    {
        Navigate($"{ServerPathBase}/{target}-antiforgery-form");

        Browser.Exists(By.Id("interactive"));

        Browser.Click(By.Id("render-form"));

        var input = Browser.Exists(By.Id("name"));
        input.SendKeys("Test");
        var submit = Browser.Exists(By.Id("submit"));
        submit.Click();

        var result = Browser.Exists(By.Id("result"));
        Browser.Equal("Test", () => result.Text);
    }
}
