// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class FormWithParentBindingContextTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public FormWithParentBindingContextTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDispatchToTheDefaultForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDispatchToTheDefaultFormWithBody(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-with-body",
            FormCssSelector = "form",
            InputFieldValue = "stranger",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindParameterToTheDefaultForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-bound-parameter",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            InputFieldId = "Parameter",
            InputFieldCssSelector = "input[name=Parameter]",
            InputFieldValue = "stranger",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindMultipleParametersToTheDefaultForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-bound-multiple-primitive-parameters",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            UpdateFormAction = () =>
            {
                Browser.Exists(By.CssSelector("input[name=Parameter]")).Clear();
                Browser.Exists(By.CssSelector("input[name=Parameter]")).SendKeys("10");

                Browser.Exists(By.CssSelector("input[name=OtherParameter]")).Clear();
                Browser.Exists(By.CssSelector("input[name=OtherParameter]")).SendKeys("true");
            },
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);

        Browser.Exists(By.Id("ParameterValue")).Text.Contains("10");
        Browser.Exists(By.Id("OtherParameterValue")).Text.Contains("True");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDisplayErrorsFromMultipleParametersToTheDefaultForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-bound-multiple-primitive-parameters",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            UpdateFormAction = () =>
            {
                Browser.Exists(By.CssSelector("input[name=Parameter]")).Clear();
                Browser.Exists(By.CssSelector("input[name=Parameter]")).SendKeys("abcd");

                Browser.Exists(By.CssSelector("input[name=OtherParameter]")).Clear();
                Browser.Exists(By.CssSelector("input[name=OtherParameter]")).SendKeys("invalid");
            },
            AssertErrors = errors =>
            {
                Assert.Collection(
                    errors,
                    error =>
                    {
                        Assert.Equal("The value 'abcd' is not valid for 'Parameter'.", error.Text);
                    },
                    error =>
                    {
                        Assert.Equal("The value 'invalid' is not valid for 'OtherParameter'.", error.Text);
                    });
                Assert.Equal("abcd", Browser.FindElement(By.CssSelector("input[name=Parameter]")).GetAttribute("value"));
                Assert.Equal("invalid", Browser.FindElement(By.CssSelector("input[name=OtherParameter]")).GetAttribute("value"));
            },
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanHandleBindingErrorsBindParameterToTheDefaultForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-bound-primitive-parameter",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            InputFieldId = "Parameter",
            InputFieldCssSelector = "input[name=Parameter]",
            InputFieldValue = "abc",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
            AssertErrors = errors =>
            {
                Assert.Collection(
                    errors,
                    error =>
                    {
                        Assert.Equal("The value 'abc' is not valid for 'Parameter'.", error.Text);
                    });
                Assert.Equal("abc", Browser.FindElement(By.CssSelector("input[name=Parameter]")).GetAttribute("value"));
            }
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindComplexTypeToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-complextype-parameter";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        var name = Browser.Exists(By.CssSelector("""input[name="Model.Name"]"""));
        name.SendKeys("John");
        var email = Browser.Exists(By.CssSelector("""input[name="Model.Email"]"""));
        email.SendKeys("john@example.com");
        Browser.Click(By.CssSelector("""input[name="Model.IsPreferred"]"""));
        Browser.Click(By.Id("send"));

        Browser.Exists(By.Id("name")).Text.Contains("John");
        Browser.Exists(By.Id("email")).Text.Contains("john@example.com");
        Browser.Exists(By.Id("preferred")).Text.Contains("True");

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDisplayBindingErrorsComplexTypeToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-complextype-parameter";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        var name = Browser.Exists(By.CssSelector("""input[name="Model.Name"]"""));
        name.SendKeys("John");
        var email = Browser.Exists(By.CssSelector("""input[name="Model.Email"]"""));
        email.SendKeys("john@example.com");
        Browser.Click(By.CssSelector("""input[name="Model.IsPreferred"]"""));
        // Set value attribute to 'invalid' to trigger validation error
        var isPreferred = Browser.Exists(By.CssSelector("""input[name="Model.IsPreferred"]"""));
        ((IJavaScriptExecutor)Browser).ExecuteScript("arguments[0].setAttribute('value', 'invalid')", isPreferred);

        Browser.Click(By.Id("send"));

        Browser.Exists(By.CssSelector("li.validation-message")).Text.Contains("The value 'invalid' is not valid for 'value'.");
        Browser.Exists(By.CssSelector("div.validation-message")).Text.Contains("The value 'invalid' is not valid for 'value'.");

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindDictionaryToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-dictionary-parameter";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        var name = Browser.Exists(By.CssSelector("""input[name="Model[Name]"]"""));
        name.SendKeys("John");
        var email = Browser.Exists(By.CssSelector("""input[name="Model[Email]"]"""));
        email.SendKeys("john@example.com");
        Browser.Click(By.CssSelector("""input[name="Model[IsPreferred]"]"""));
        Browser.Click(By.Id("send"));

        Browser.Exists(By.Id("name")).Text.Contains("John");
        Browser.Exists(By.Id("email")).Text.Contains("john@example.com");
        Browser.Exists(By.Id("preferred")).Text.Contains("True");

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDisplayBindingErrorsDictionaryToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-dictionary-parameter-errors";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        var name = Browser.Exists(By.CssSelector("""input[name="Model[Name]"]"""));
        ((IJavaScriptExecutor)Browser).ExecuteScript("arguments[0].setAttribute('value', 'name')", name);
        var email = Browser.Exists(By.CssSelector("""input[name="Model[Email]"]"""));
        ((IJavaScriptExecutor)Browser).ExecuteScript("arguments[0].setAttribute('value', 'email')", email);
        var preferred = Browser.Exists(By.CssSelector("""input[name="Model[IsPreferred]"]"""));
        ((IJavaScriptExecutor)Browser).ExecuteScript("arguments[0].setAttribute('value', 'preferred')", preferred);

        Browser.Click(By.Id("send"));

        Browser.Exists(By.CssSelector("[data-name='Name']"));
        Browser.Exists(By.CssSelector("[data-email='Email']"));
        Browser.Exists(By.CssSelector("[data-preferred='IsPreferred']"));

        Browser.Equal(3, () => Browser.FindElements(By.CssSelector("li.validation-message")).Count());

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindCollectionsToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-collection-parameter";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        for (var i = 0; i < 2; i++)
        {
            var name = Browser.Exists(By.CssSelector($"""input[name="Model[{i}].Name"]"""));
            name.Clear();
            name.SendKeys($"John{i + 4}");
            var email = Browser.Exists(By.CssSelector($"""input[name="Model[{i}].Email"]"""));
            email.Clear();
            email.SendKeys($"john{i + 4}@example.com");
            Browser.Click(By.CssSelector($"""input[name="Model[{i}].IsPreferred"]"""));
        }

        Browser.Click(By.Id("send"));

        for (var i = 0; i < 2; i++)
        {
            Browser.Exists(By.Id($"name[{i}]")).Text.Contains($"John{i + 4}");
            Browser.Exists(By.Id($"email[{i}]")).Text.Contains($"john{i + 4}@example.com");
            Browser.Exists(By.Id($"preferred[{i}]")).Text.Contains(i % 2 == 0 ? "True" : "False");
        }

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDisplayBindingErrorsCollectionsToDefaultForm(bool suppressEnhancedNavigation)
    {
        var url = "forms/default-form-bound-collection-parameter";
        var expectedTarget = GetExpectedTarget(this, null, url);

        if (suppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

        GoTo(url);

        Browser.Exists(By.Id("ready"));
        var form = Browser.Exists(By.CssSelector("form"));
        var formTarget = form.GetAttribute("action");
        var actionValue = form.GetDomAttribute("action");
        Assert.Equal(expectedTarget, formTarget);
        Assert.Null(actionValue);

        for (var i = 0; i < 2; i++)
        {
            var name = Browser.Exists(By.CssSelector($"""input[name="Model[{i}].Name"]"""));
            name.Clear();
            name.SendKeys($"John{i + 4}");
            var email = Browser.Exists(By.CssSelector($"""input[name="Model[{i}].Email"]"""));
            email.Clear();
            email.SendKeys($"john{i + 4}@example.com");
            var preferredCriteria = By.CssSelector($"""input[name="Model[{i}].IsPreferred"]""");
            var preferred = Browser.FindElement(preferredCriteria);
            if (!preferred.Selected)
            {
                Browser.Click(preferredCriteria);
            }
            // Set the value for preferred ti 'invalid' to trigger a binding error
            ((IJavaScriptExecutor)Browser).ExecuteScript($"arguments[0].setAttribute('value', 'invalid{i}')", preferred);
        }

        Browser.Click(By.Id("send"));

        Browser.Exists(By.CssSelector("[data-index='0']")).Text.Contains("The value 'invalid0' is not valid for 'IsPreferred'.");
        Browser.Exists(By.CssSelector("[data-index='1']")).Text.Contains("The value 'invalid1' is not valid for 'IsPreferred'.");

        Browser.Equal(2, () => Browser.FindElements(By.CssSelector("li.validation-message")).Count());

        if (!suppressEnhancedNavigation)
        {
            // Verify the same form element is still in the page
            // We wouldn't be allowed to read the attribute if the element is stale
            Assert.Equal(expectedTarget, form.GetAttribute("action"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanHandleBindingErrorsBindParameterToNamedForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/named-form-bound-primitive-parameter",
            FormCssSelector = "form[name=named-form-handler]",
            ExpectedActionValue = "forms/named-form-bound-primitive-parameter?handler=named-form-handler",
            InputFieldId = "Parameter",
            InputFieldCssSelector = "input[name=Parameter]",
            InputFieldValue = "abc",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
            AssertErrors = errors =>
            {
                Assert.Collection(
                    errors,
                    error =>
                    {
                        Assert.Equal("The value 'abc' is not valid for 'Parameter'.", error.Text);
                    });
                Assert.Equal("abc", Browser.FindElement(By.CssSelector("input[name=Parameter]")).GetAttribute("value"));
            }
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanReadFormValuesDuringOnInitialized(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/default-form-with-body-on-initialized",
            FormCssSelector = "form",
            InputFieldValue = "stranger",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDispatchToNamedForm(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/named-form",
            FormCssSelector = "form[name=named-form-handler]",
            ExpectedActionValue = "forms/named-form?handler=named-form-handler",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindFormValueFromNamedFormWithBody(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/named-form-bound-parameter",
            FormCssSelector = "form[name=named-form-handler]",
            ExpectedActionValue = "forms/named-form-bound-parameter?handler=named-form-handler",
            InputFieldId = "Parameter",
            InputFieldCssSelector = "input[name=Parameter]",
            InputFieldValue = "stranger",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDispatchToNamedFormInNestedContext(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/nested-named-form",
            FormCssSelector = "form[name=\"parent-context.named-form-handler\"]",
            ExpectedActionValue = "forms/nested-named-form?handler=parent-context.named-form-handler",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanBindFormValueFromNestedNamedFormWithBody(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/nested-named-form-bound-parameter",
            FormCssSelector = """form[name="parent-context.named-form-handler"]""",
            ExpectedActionValue = "forms/nested-named-form-bound-parameter?handler=parent-context.named-form-handler",
            InputFieldId = "Parameter",
            InputFieldCssSelector = "input[name=Parameter]",
            InputFieldValue = "stranger",
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanDispatchToFormDefinedInNonPageComponent(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/form-defined-inside-component",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DispatchingToAmbiguousFormFails(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/ambiguous-forms",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            DispatchEvent = true,
            SubmitButtonId = "send-second",
            ShouldCauseInternalServerError = true,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
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
            ShouldCauseInternalServerError = true,
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
            ShouldCauseInternalServerError = true,
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

    [Theory]
    [InlineData(true)]
    [InlineData(false, Skip = "https://github.com/dotnet/aspnetcore/issues/49115")]
    public void FormNoAntiforgeryReturnBadRequest(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/no-antiforgery",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            ShouldCauseBadRequest = true,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FormAntiforgeryCheckDisabledOnPage(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/disable-antiforgery-check",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Fact]
    public void FormCanAddAntiforgeryAfterTheResponseHasStarted()
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/antiforgery-after-response-started",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = true,
        };
        DispatchToFormCore(dispatchToForm);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FormElementWithAntiforgery(bool suppressEnhancedNavigation)
    {
        var dispatchToForm = new DispatchToForm(this)
        {
            Url = "forms/form-element-antiforgery",
            FormCssSelector = "form",
            ExpectedActionValue = null,
            SuppressEnhancedNavigation = suppressEnhancedNavigation,
        };
        DispatchToFormCore(dispatchToForm);
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
        if (dispatch.SuppressEnhancedNavigation)
        {
            GoTo("");
            Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
        }

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

        if (dispatch.UpdateFormAction != null)
        {
            dispatch.UpdateFormAction();
        }
        else if (dispatch.InputFieldValue != null)
        {
            var criteria = dispatch.InputFieldCssSelector != null ?
                By.CssSelector(dispatch.InputFieldCssSelector) :
                By.Id(dispatch.InputFieldId);

            Browser.Exists(criteria).Clear();
            Browser.Exists(criteria).SendKeys(dispatch.InputFieldValue);
        }

        Browser.Click(By.Id(dispatch.SubmitButtonId));

        if (dispatch.ShouldCauseInternalServerError)
        {
            if (dispatch.SuppressEnhancedNavigation)
            {
                // Chrome's built-in error UI for a 500 response when there's no response content
                Browser.Exists(By.Id("main-frame-error"));
            }
            else
            {
                // The UI generated by enhanced nav when there's no response content
                Browser.Contains("Error: 500", () => Browser.Exists(By.TagName("html")).Text);
            }
        }
        else if (dispatch.ShouldCauseBadRequest)
        {
            if (dispatch.SuppressEnhancedNavigation)
            {
                // Chrome's built-in error UI for a 500 response when there's no response content
                Browser.Contains("HTTP ERROR 400", () => Browser.Exists(By.CssSelector("div.error-code")).Text);
            }
            else
            {
                // The UI generated by enhanced nav when there's no response content
                Browser.Contains("Error: 400", () => Browser.Exists(By.TagName("html")).Text);
            }
        }
        else if (dispatch.ShouldCauseBindingErrors)
        {
            var errors = Browser.FindElements(By.CssSelector("#errors > li"));
            dispatch.AssertErrors(errors);
        }
        else
        {
            if (dispatch.UpdateFormAction == null)
            {
                var text = Browser.Exists(By.Id(dispatch.SubmitPassId)).Text;
                if (dispatch.InputFieldValue != null)
                {
                    Assert.Equal($"Hello {dispatch.InputFieldValue}!", text);
                }
            }

            if (!dispatch.SuppressEnhancedNavigation)
            {
                // Verify the same form element is still in the page
                // We wouldn't be allowed to read the attribute if the element is stale
                Assert.Equal(dispatch.ExpectedTarget, form.GetAttribute("action"));
            }
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
        public bool ShouldCauseInternalServerError { get; internal set; }
        public bool ShouldCauseBadRequest { get; internal set; }
        public bool ShouldCauseBindingErrors => AssertErrors != null;
        public bool SuppressEnhancedNavigation { get; internal set; }
        public Action UpdateFormAction { get; internal set; }
        public Action<ReadOnlyCollection<IWebElement>> AssertErrors { get; internal set; }
    }

    private string GetExpectedTarget(FormWithParentBindingContextTest test, string expectedActionValue, string url)
        => $"{new Uri(test._serverFixture.RootUri, test.ServerPathBase)}/{expectedActionValue ?? url}";

    private void GoTo(string relativePath)
    {
        Navigate($"{ServerPathBase}/{relativePath}");
    }
}
