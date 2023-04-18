// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class FormHandlingTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup>>
{
    public FormHandlingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    // Can dispatch to the default form
    // Can dispatch to a named form
    // Rendering ambiguous forms doesn't cause an error.
    // Dispatching to ambiguous forms raises an error.
    // Can dispatch to a nested form
    // Can dispatch to a nested named form inside the default binding context.

    [Fact]
    public void CanDispatchToTheDefaultForm()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form",
            FormCssSelector = "form",
            ExpectedActionValue = null,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanDispatchToNamedForm()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/named-form",
            FormCssSelector = "form[name=named-form-handler]",
            ExpectedActionValue = "forms/named-form?handler=named-form-handler",
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanDispatchToNamedFormInNestedContext()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/nested-named-form",
            FormCssSelector = "form[name=\"parent-context.named-form-handler\"]",
            ExpectedActionValue = "forms/nested-named-form?handler=parent-context.named-form-handler",
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanDispatchToFormDefinedInNonPageComponent()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "/forms/form-defined-inside-component",
            FormCssSelector = "form",
            ExpectedActionValue = "",
        };
        DispatchToFormCore(dispatchToForm);
    }

    private void DispatchToFormCore(DispatchToForm dispatch)
    {
        GoTo(dispatch.Url);

        Browser.Exists(By.Id(dispatch.Ready));
        var form = Browser.Exists(By.CssSelector(dispatch.FormCssSelector));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(dispatch.ExpectedTarget, formTarget);
        Assert.Equal(dispatch.ExpectedActionValue, actionValue);

        Browser.Exists(By.Id("send")).Click();

        Browser.Exists(By.Id(dispatch.SubmitPassId));
    }

    private record struct DispatchToForm()
    {
        public DispatchToForm(FormHandlingTest test) : this()
        {
            Base = new Uri(test._serverFixture.RootUri, test.ServerPathBase).ToString();
        }

        public string Base;
        public string Url;
        public string Ready = "ready";
        public string SubmitPassId = "pass";
        public string FormCssSelector;
        public string ExpectedActionValue;
        public string ExpectedTarget => $"{Base}/{ExpectedActionValue ?? Url}";

    }

    private void GoTo(string relativePath)
    {
        Navigate($"{ServerPathBase}/{relativePath}");
    }
}
