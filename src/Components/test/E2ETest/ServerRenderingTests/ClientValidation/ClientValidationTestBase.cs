// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.ClientValidation;

// Shared base for the ClientValidation Selenium tests. Centralizes navigation +
// readiness waits + the submit interception that prevents the test forms from
// actually POSTing to the server.
//
// Why the submit interception:
// The test pages render plain SSR forms with raw data-val-* markup and no
// anti-forgery token. When the JS client validation library accepts a submit
// (form is valid), the browser would POST to the SSR endpoint, hit a 400, and
// navigate to a Chromium error page. Subsequent assertions on the original page
// would then fail or, worse, succeed flakily by reading whichever page Selenium
// happened to land on. We install a document-level bubble-phase preventDefault:
//   - Invalid submits: the validation library handler runs in capture phase on
//     document, calls preventDefault + stopPropagation, and the bubble phase is
//     never reached. No interaction with our handler.
//   - Valid submits: the library lets the event through; the page's own
//     'validationcomplete' bubble listeners on the form element fire first; then
//     our document-bubble handler cancels the actual POST.
public abstract class ClientValidationTestBase
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    protected ClientValidationTestBase(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    // Navigates to a /forms/client-validation/<page> URL, waits for Blazor to
    // start, and (by default) installs the submit interceptor. Set
    // expectTrackedForm to false for pages that intentionally have no
    // [data-val=true] elements (e.g. the 'no-validation' page) so the helper
    // does not block waiting for form[novalidate] that will never appear. Set
    // interceptSubmit to false on pages whose purpose is to verify native form
    // submission (e.g. 'no-validation') so the interceptor does not mask the
    // behaviour the test is asserting on.
    protected void NavigateToClientValidationPage(string page, bool expectTrackedForm = true, bool interceptSubmit = true)
    {
        Navigate($"subdir/forms/client-validation/{page}");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Exists(By.Id("page-title"));
        if (expectTrackedForm)
        {
            Browser.Exists(By.CssSelector("form[novalidate]"));
        }

        if (interceptSubmit)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript(
                "document.addEventListener('submit', function (e) { e.preventDefault(); }, false);");
        }
    }
}
