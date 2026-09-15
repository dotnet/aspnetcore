// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[UITest]
public partial class PredictiveEmptyDocumentTests : DojoTestBase
{
    [TestMethod]
    [DojoBackends]
    public async Task EmptyPrediction_ClearsTheDocumentAfterConfirmation(DojoBackendKind backend)
    {
        var dojo = await GetDojoAsync(backend, DojoRecording.PredictiveStateUpdates);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(dojo.UI));
        var page = await context.NewPageAsync();
        await page.GotoAsync(dojo.GetScenarioUrl("/predictive_state_updates"));
        await page.WaitForInteractiveAsync("[aria-label='Document editor']");
        var editor = page.GetByRole(AriaRole.Textbox, new() { Name = "Document editor", Exact = true });
        await editor.FillAsync("Document to remove.");
        await page.Locator("textarea.sc-ai-input__textarea").FillAsync("Please clear the document.");
        await page.Locator("button.sc-ai-input__send").ClickAsync();

        var confirmation = page.Locator(".confirm-changes");
        await Expect(confirmation).ToBeVisibleAsync();
        await Expect(editor).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(editor.Locator("s")).ToHaveTextAsync("Document to remove.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();

        await Expect(editor).ToHaveClassAsync("document-editor__input");
        await Expect(editor).ToHaveValueAsync("");
        await Expect(page.Locator(".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToHaveTextAsync("The document is empty.");
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }
}
