// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class ElementReferenceDisposalReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public ElementReferenceDisposalReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true", "--DisableCircuitPersistence", "true");
    }

    [Fact]
    public void RejectedCircuitRecoveryReleasesPendingServerElementReferences()
    {
        Navigate($"{ServerPathBase}/nav/element-reference-disposal");

        var ids = new[] { "server", "server-child", "webassembly", "webassembly-child" };
        foreach (var id in ids)
        {
            Browser.Equal("True", () => Browser.Exists(By.Id($"element-reference-disposal-initialized-{id}")).Text);
        }

        try
        {
            Browser.Click(By.Id("element-reference-disposal-navigation"));
            Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
            Browser.True(() => Browser.ExecuteJavaScript<long>(
                "return window.elementReferenceDisposalTest.acknowledgedRemovals") > 0);
            foreach (var id in ids)
            {
                Browser.Equal("true", () => Browser.ExecuteJavaScript<string>(
                    $"return sessionStorage.getItem('element-reference-disposal-pending-{id}')"));
                Assert.False(ReferenceIsMissing(id));
            }

            // Use the custom reconnect display so rejection does not reload the document.
            Assert.False((bool)((IJavaScriptExecutor)Browser).ExecuteAsyncScript(
                "const done = arguments[0];" +
                "const modal = document.createElement('div'); modal.id = 'components-reconnect-modal'; document.body.append(modal);" +
                "Blazor._internal.forceCloseConnection().then(() => {" +
                "  const test = window.elementReferenceDisposalTest; done(test.referenceIsMissing(test.captures.get('server')));" +
                "});"));
            Browser.Exists(By.CssSelector("#components-reconnect-modal.components-reconnect-rejected"));

            Assert.True(ReferenceIsMissing("server"));
            Assert.True(ReferenceIsMissing("server-child"));
            Assert.False(ReferenceIsMissing("webassembly"));
            Assert.False(ReferenceIsMissing("webassembly-child"));
            Browser.True(() => Browser.ExecuteJavaScript<long>(
                "return window.elementReferenceDisposalTest.circuitDisposals") > 0);
        }
        finally
        {
            Browser.ExecuteJavaScript(
                "import('./_content/TestContentPackage/ElementReferenceDisposal.razor.js').then(module => module.releaseAllDisposals());");
        }

        bool ReferenceIsMissing(string id) => Browser.ExecuteJavaScript<bool>(
            $"return window.elementReferenceDisposalTest.referenceIsMissing(window.elementReferenceDisposalTest.captures.get('{id}'))");
    }
}
