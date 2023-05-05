// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using OpenQA.Selenium;
using Components.TestServer.RazorComponents;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class NamedFormNoParentBindingContextTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<NamedFormContextNoFormContextApp>>>
{
    public NamedFormNoParentBindingContextTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<NamedFormContextNoFormContextApp>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanDispatchToNamedFormNoParentBindingContext()
    {
        Navigate(ServerPathBase);

        Browser.Exists(By.Id("ready"));

        var form = Browser.Exists(By.CssSelector("form[name=named-form-handler]"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        var baseUri = new Uri(_serverFixture.RootUri, ServerPathBase).ToString();

        Assert.Equal($"{baseUri}/?handler=named-form-handler", formTarget);
        Assert.Equal("?handler=named-form-handler", actionValue);

        Browser.Click(By.Id("send"));
        Browser.Exists(By.Id("pass"));
    }
}
