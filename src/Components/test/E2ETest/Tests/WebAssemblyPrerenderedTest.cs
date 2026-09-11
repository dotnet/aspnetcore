// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyPrerenderedTest : ServerTestBase<TrimmingServerFixture<Wasm.Prerendered.Server.Startup>>
{
    public WebAssemblyPrerenderedTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<Wasm.Prerendered.Server.Startup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.Environment = AspNetEnvironment.Development;
    }

    [Fact]
    public void CanPrerenderAndHydrateHeadOutletWithoutRemovingTitle()
    {
        Navigate("/");

        // Verify that the title is updated during prerendering
        Browser.Equal("Current count: 0", () => Browser.Title);

        var javascript = (IJavaScriptExecutor)Browser;
        // Observe before Blazor starts so every title mutation during hydration is captured.
        javascript.ExecuteScript("""
            window['__aspnetcore__testing__title_was_missing__'] = false;
            window['__aspnetcore__testing__title_observer__'] = new MutationObserver(records => {
                let removedTitle = false;
                let addedTitle = false;
                for (const record of records) {
                    for (const node of record.removedNodes) {
                        if (node.nodeName === 'TITLE') {
                            removedTitle = true;
                        }
                    }
                    for (const node of record.addedNodes) {
                        if (node.nodeName === 'TITLE') {
                            addedTitle = true;
                        }
                    }
                }

                if (removedTitle && !addedTitle && document.head.querySelector('title') === null) {
                    window['__aspnetcore__testing__title_was_missing__'] = true;
                }
            });
            window['__aspnetcore__testing__title_observer__'].observe(document.head, { childList: true, subtree: true });
            """);

        Browser.Click(By.Id("start-blazor"));

        WaitUntilLoaded();

        Browser.Equal("Current count: 0", () => Browser.Title);
        Browser.False(() => (bool)javascript.ExecuteScript("return window['__aspnetcore__testing__title_was_missing__'];"));
        Browser.True(() => (bool)javascript.ExecuteScript("return document.head.querySelectorAll('title').length === 1;"));
        javascript.ExecuteScript("window['__aspnetcore__testing__title_observer__'].disconnect();");

        // Verify that the HeadOutlet root component was added after prerendering
        Browser.Click(By.Id("increment-count"));
        Browser.Equal("Current count: 1", () => Browser.Title);
        Browser.True(() => (bool)javascript.ExecuteScript("return document.head.querySelectorAll('title').length === 1;"));
    }

    private void WaitUntilLoaded()
    {
        var jsExecutor = (IJavaScriptExecutor)Browser;
        Browser.True(() => jsExecutor.ExecuteScript("return window['__aspnetcore__testing__blazor_wasm__started__'];") is not null);
    }
}
