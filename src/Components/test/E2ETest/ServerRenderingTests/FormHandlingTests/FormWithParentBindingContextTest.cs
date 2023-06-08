// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class FormWithParentBindingContextTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<FormWithDefaultContextApp>>>
{
    public FormWithParentBindingContextTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<FormWithDefaultContextApp>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

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
    public void CanDispatchToTheDefaultFormWithBody()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-with-body",
            FormCssSelector = "form",
            InputFieldValue = "stranger",
            ExpectedActionValue = null,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanBindParameterToTheDefaultForm()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-bound-parameter",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            InputFieldId = "value",
            InputFieldCssSelector = "input[name=value]",
            InputFieldValue = "stranger",
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanReadFormValuesDuringOnInitialized()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-with-body-on-initialized",
            FormCssSelector = "form",
            InputFieldValue = "stranger",
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
    public void CanBindFormValueFromNamedFormWithBody()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/named-form-bound-parameter",
            FormCssSelector = "form[name=named-form-handler]",
            ExpectedActionValue = "forms/named-form-bound-parameter?handler=named-form-handler",
            InputFieldId = "value",
            InputFieldCssSelector = "input[name=value]",
            InputFieldValue = "stranger",
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
    public void CanBindFormValueFromNestedNamedFormWithBody()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/nested-named-form-bound-parameter",
            FormCssSelector = """form[name="parent-context.named-form-handler"]""",
            ExpectedActionValue = "forms/nested-named-form-bound-parameter?handler=parent-context.named-form-handler",
            InputFieldId = "value",
            InputFieldCssSelector = "input[name=value]",
            InputFieldValue = "stranger",
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanDispatchToFormDefinedInNonPageComponent()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/form-defined-inside-component",
            FormCssSelector = "form",
            ExpectedActionValue = null,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanRenderAmbiguousForms()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/ambiguous-forms",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            DispatchEvent = false
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void DispatchingToAmbiguousFormFails()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/ambiguous-forms",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            DispatchEvent = true,
            SubmitButtonId = "send-second",
            // This is an error ID on the page chrome shows from a 500.
            SubmitPassId = "main-frame-error"
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void CanDispatchToFormRenderedAsynchronously()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/async-rendered-form",
            FormCssSelector = "form",
            ExpectedActionValue = null
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void FormThatDisappearsBeforeQuiesceDoesNotBind()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/disappears-before-dispatching",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SubmitButtonId = "test-send",
            SubmitPassId = "main-frame-error"
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void ChangingComponentsToDispatchBeforeQuiesceDoesNotBind()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/switching-components-does-not-bind",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SubmitPassId = "main-frame-error"
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public async Task CanPostFormsWithStreamingRenderingAsync()
    {
        GoTo("forms/streaming-rendering/CanPostFormsWithStreamingRendering");

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var actionValue = form.GetDomAttribute("action");
        Assert.Null(actionValue);

        Browser.Click(By.Id("send"));

        Browser.Exists(By.Id("progress"));

        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.PostAsync("subdir/forms/streaming-rendering/complete/CanPostFormsWithStreamingRendering", content: null);
        response.EnsureSuccessStatusCode();

        Browser.Exists(By.Id("pass"));
    }

    [Fact]
    public async Task CanModifyTheHttpResponseDuringEventHandling()
    {
        GoTo("forms/modify-http-context/ModifyHttpContext");

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var actionValue = form.GetDomAttribute("action");
        Assert.Null(actionValue);

        Browser.Click(By.Id("send"));

        Browser.Exists(By.Id("progress"));

        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.PostAsync("subdir/forms/streaming-rendering/complete/ModifyHttpContext", content: null);
        response.EnsureSuccessStatusCode();

        Browser.Exists(By.Id("pass"));
        var cookie = Browser.Manage().Cookies.GetCookieNamed("operation");
        Assert.Equal("ModifyHttpContext", cookie.Value);
    }

    [Fact]
    public async Task CanHandleFormPostNonStreamingRenderingAsyncHandler()
    {
        GoTo("forms/non-streaming-async-form-handler/CanHandleFormPostNonStreamingRenderingAsyncHandler");

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var actionValue = form.GetDomAttribute("action");
        Assert.Null(actionValue);

        Browser.Click(By.Id("send"));

        await Task.Yield();

        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.PostAsync("subdir/forms/streaming-rendering/complete/CanHandleFormPostNonStreamingRenderingAsyncHandler", content: null);
        response.EnsureSuccessStatusCode();

        Browser.Exists(By.Id("pass"));
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

        if (!dispatch.DispatchEvent)
        {
            return;
        }

        if (dispatch.InputFieldValue != null)
        {
            var criteria = dispatch.InputFieldCssSelector != null ?
                By.CssSelector(dispatch.InputFieldCssSelector) :
                By.Id(dispatch.InputFieldId);

            Browser.Exists(criteria).SendKeys(dispatch.InputFieldValue);
        }

        Browser.Click(By.Id(dispatch.SubmitButtonId));

        var text = Browser.Exists(By.Id(dispatch.SubmitPassId)).Text;
        if (dispatch.InputFieldValue != null)
        {
            Assert.Equal($"Hello {dispatch.InputFieldValue}!", text);
        }
    }

    private record struct DispatchToForm()
    {
        public DispatchToForm(FormWithParentBindingContextTest test) : this()
        {
            Base = new Uri(test._serverFixture.RootUri, test.ServerPathBase).ToString();
        }

        public string Base;
        public string Url;
        public string SubmitPassId = "pass";
        public string Ready = "ready";
        public string FormCssSelector;
        public string ExpectedActionValue;
        public string InputFieldValue;

        public string ExpectedTarget => $"{Base}/{ExpectedActionValue ?? Url}";

        public bool DispatchEvent { get; internal set; } = true;

        public string SubmitButtonId { get; internal set; } = "send";
        public string InputFieldId { get; internal set; } = "firstName";
        public string InputFieldCssSelector { get; internal set; } = null;
    }

    private void GoTo(string relativePath)
    {
        Navigate($"{ServerPathBase}/{relativePath}");
    }
}
