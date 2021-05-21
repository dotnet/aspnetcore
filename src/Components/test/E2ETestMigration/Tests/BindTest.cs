// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BindTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public BindTest(
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        protected override Type TestComponent { get; } = typeof(BindCasesComponent);

        protected override async Task InitializeCoreAsync(TestContext context)
        {
            await base.InitializeCoreAsync(context);

            // On WebAssembly, page reloads are expensive so skip if possible
            //Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        protected async Task<string> GetInputValue(string selector)
            => await TestPage.EvalOnSelectorAsync<string>(selector, "e => e.value");

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        [InlineData(BrowserKind.Firefox)]
        [InlineData(BrowserKind.Webkit)]
        // NOTE: BrowserKind argument must be first
        public async Task CanBindTextbox_InitiallyBlank(BrowserKind browserKind)
        {
            if (ShouldSkip(browserKind)) 
            {
                return;
            }

            var target = await TestPage.QuerySelectorAsync("#textbox-initially-blank");
            Assert.Equal(string.Empty, await GetInputValue("#textbox-initially-blank"));
            Assert.Equal(string.Empty, await TestPage.GetInnerTextAsync("#textbox-initially-blank-value"));
            Assert.Equal(string.Empty, await GetInputValue("#textbox-initially-blank-mirror"));

            // Modify target; verify value is updated and that textboxes linked to the same data are updated
            await target.TypeAsync("Changed value");
            // Doesn't update until change event
            Assert.Equal(string.Empty, await TestPage.GetInnerTextAsync("#textbox-initially-blank-value"));
            Assert.Equal(string.Empty, await GetInputValue("#textbox-initially-blank-mirror"));
            await target.PressAsync("Tab");
            Assert.Equal("Changed value", await TestPage.GetInnerTextAsync("#textbox-initially-blank-value"));
            Assert.Equal("Changed value", await GetInputValue("#textbox-initially-blank-mirror"));

            // Remove the value altogether
            await TestPage.ClickAsync("#textbox-initially-blank-setnull");
            Assert.Equal(string.Empty, await GetInputValue("#textbox-initially-blank"));
            Assert.Equal(string.Empty, await TestPage.GetInnerTextAsync("#textbox-initially-blank-value"));
            Assert.Equal(string.Empty, await GetInputValue("#textbox-initially-blank-mirror"));
        }

        //[Fact]
        //public void CanBindTextbox_InitiallyPopulated()
        //{
        //    var target = Browser.Exists(By.Id("textbox-initially-populated"));
        //    var boundValue = Browser.Exists(By.Id("textbox-initially-populated-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-initially-populated-mirror"));
        //    var setNullButton = Browser.Exists(By.Id("textbox-initially-populated-setnull"));
        //    Assert.Equal("Hello", target.GetAttribute("value"));
        //    Assert.Equal("Hello", boundValue.Text);
        //    Assert.Equal("Hello", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("Changed value\t");
        //    Browser.Equal("Changed value", () => boundValue.Text);
        //    Assert.Equal("Changed value", mirrorValue.GetAttribute("value"));

        //    // Remove the value altogether
        //    setNullButton.Click();
        //    Browser.Equal(string.Empty, () => target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextbox_WithBindSuffixInitiallyPopulated()
        //{
        //    var target = Browser.Exists(By.Id("bind-with-suffix-textbox-initially-populated"));
        //    var boundValue = Browser.Exists(By.Id("bind-with-suffix-textbox-initially-populated-value"));
        //    var mirrorValue = Browser.Exists(By.Id("bind-with-suffix-textbox-initially-populated-mirror"));
        //    var setNullButton = Browser.Exists(By.Id("bind-with-suffix-textbox-initially-populated-setnull"));
        //    Assert.Equal("Hello", target.GetAttribute("value"));
        //    Assert.Equal("Hello", boundValue.Text);
        //    Assert.Equal("Hello", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("Changed value\t");
        //    Browser.Equal("Changed value", () => boundValue.Text);
        //    Assert.Equal("Changed value", mirrorValue.GetAttribute("value"));

        //    // Remove the value altogether
        //    setNullButton.Click();
        //    Browser.Equal(string.Empty, () => target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextArea_InitiallyBlank()
        //{
        //    var target = Browser.Exists(By.Id("textarea-initially-blank"));
        //    var boundValue = Browser.Exists(By.Id("textarea-initially-blank-value"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.SendKeys("Changed value");
        //    Assert.Equal(string.Empty, boundValue.Text); // Don't update as there's no change event fired yet.
        //    target.SendKeys("\t");
        //    Browser.Equal("Changed value", () => boundValue.Text);
        //}

        //[Fact]
        //public void CanBindTextArea_InitiallyPopulated()
        //{
        //    var target = Browser.Exists(By.Id("textarea-initially-populated"));
        //    var boundValue = Browser.Exists(By.Id("textarea-initially-populated-value"));
        //    Assert.Equal("Hello", target.GetAttribute("value"));
        //    Assert.Equal("Hello", boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.Clear();
        //    target.SendKeys("Changed value\t");
        //    Browser.Equal("Changed value", () => boundValue.Text);
        //}

        //[Fact]
        //public void CanBindCheckbox_InitiallyNull()
        //{
        //    var target = Browser.Exists(By.Id("checkbox-initially-null"));
        //    var boundValue = Browser.Exists(By.Id("checkbox-initially-null-value"));
        //    var invertButton = Browser.Exists(By.Id("checkbox-initially-null-invert"));
        //    Assert.False(target.Selected);
        //    Assert.Equal(string.Empty, boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.Click();
        //    Browser.True(() => target.Selected);
        //    Browser.Equal("True", () => boundValue.Text);

        //    // Modify data; verify checkbox is updated
        //    invertButton.Click();
        //    Browser.False(() => target.Selected);
        //    Browser.Equal("False", () => boundValue.Text);
        //}

        //[Fact]
        //public void CanBindCheckbox_InitiallyUnchecked()
        //{
        //    var target = Browser.Exists(By.Id("checkbox-initially-unchecked"));
        //    var boundValue = Browser.Exists(By.Id("checkbox-initially-unchecked-value"));
        //    var invertButton = Browser.Exists(By.Id("checkbox-initially-unchecked-invert"));
        //    Assert.False(target.Selected);
        //    Assert.Equal("False", boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.Click();
        //    Browser.True(() => target.Selected);
        //    Browser.Equal("True", () => boundValue.Text);

        //    // Modify data; verify checkbox is updated
        //    invertButton.Click();
        //    Browser.False(() => target.Selected);
        //    Browser.Equal("False", () => boundValue.Text);
        //}

        //[Fact]
        //public void CanBindCheckbox_InitiallyChecked()
        //{
        //    var target = Browser.Exists(By.Id("checkbox-initially-checked"));
        //    var boundValue = Browser.Exists(By.Id("checkbox-initially-checked-value"));
        //    var invertButton = Browser.Exists(By.Id("checkbox-initially-checked-invert"));
        //    Assert.True(target.Selected);
        //    Assert.Equal("True", boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.Click();
        //    Browser.False(() => target.Selected);
        //    Browser.Equal("False", () => boundValue.Text);

        //    // Modify data; verify checkbox is updated
        //    invertButton.Click();
        //    Browser.True(() => target.Selected);
        //    Browser.Equal("True", () => boundValue.Text);
        //}

        //[Fact]
        //public void CanBindSelect()
        //{
        //    var target = new SelectElement(Browser.Exists(By.Id("select-box")));
        //    var boundValue = Browser.Exists(By.Id("select-box-value"));
        //    Assert.Equal("Second choice", target.SelectedOption.Text);
        //    Assert.Equal("Second", boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.SelectByText("Third choice");
        //    Browser.Equal("Third", () => boundValue.Text);

        //    // Also verify we can add and select new options atomically
        //    // Don't move this into a separate test, because then the previous assertions
        //    // would be dependent on test execution order (or would require a full page reload)
        //    Browser.Exists(By.Id("select-box-add-option")).Click();
        //    Browser.Equal("Fourth", () => boundValue.Text);
        //    Assert.Equal("Fourth choice", target.SelectedOption.Text);

        //    // verify that changing an option value and selected value at the same time works.
        //    Browser.Exists(By.Id("change-variable-value")).Click();
        //    Browser.Equal("Sixth", () => boundValue.Text);

        //    // Verify we can select options whose value is empty
        //    // https://github.com/dotnet/aspnetcore/issues/17735
        //    target.SelectByText("Empty value");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Browser.Equal("Empty value", () => target.SelectedOption.Text);
        //}

        //[Fact]
        //public void CanBindSelectToMarkup()
        //{
        //    var target = new SelectElement(Browser.Exists(By.Id("select-markup-box")));
        //    var boundValue = Browser.Exists(By.Id("select-markup-box-value"));
        //    Assert.Equal("Second choice", target.SelectedOption.Text);
        //    Assert.Equal("Second", boundValue.Text);

        //    // Modify target; verify value is updated
        //    target.SelectByText("Third choice");
        //    Browser.Equal("Third", () => boundValue.Text);

        //    // Verify we can select options whose value is empty
        //    // https://github.com/dotnet/aspnetcore/issues/17735
        //    target.SelectByText("Empty value");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Browser.Equal("Empty value", () => target.SelectedOption.Text);
        //}

        //[Fact]
        //public void CanBindTextboxInt()
        //{
        //    var target = Browser.Exists(By.Id("textbox-int"));
        //    var boundValue = Browser.Exists(By.Id("textbox-int-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-int-mirror"));
        //    Assert.Equal("-42", target.GetAttribute("value"));
        //    Assert.Equal("-42", boundValue.Text);
        //    Assert.Equal("-42", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to zero
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Leading zeros are not preserved
        //    target.SendKeys("42");
        //    Browser.Equal("042", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal("42", () => target.GetAttribute("value"));
        //    Assert.Equal("42", boundValue.Text);
        //    Assert.Equal("42", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableInt()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-int"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-int-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-int-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("-42\t");
        //    Browser.Equal("-42", () => boundValue.Text);
        //    Assert.Equal("-42", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("42\t");
        //    Browser.Equal("42", () => boundValue.Text);
        //    Assert.Equal("42", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxLong()
        //{
        //    var target = Browser.Exists(By.Id("textbox-long"));
        //    var boundValue = Browser.Exists(By.Id("textbox-long-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-long-mirror"));
        //    Assert.Equal("3000000000", target.GetAttribute("value"));
        //    Assert.Equal("3000000000", boundValue.Text);
        //    Assert.Equal("3000000000", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to zero
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("-3000000000\t");
        //    Browser.Equal("-3000000000", () => target.GetAttribute("value"));
        //    Assert.Equal("-3000000000", boundValue.Text);
        //    Assert.Equal("-3000000000", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableLong()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-long"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-long-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-long-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("3000000000\t");
        //    Browser.Equal("3000000000", () => boundValue.Text);
        //    Assert.Equal("3000000000", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("-3000000000\t");
        //    Browser.Equal("-3000000000", () => boundValue.Text);
        //    Assert.Equal("-3000000000", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxShort()
        //{
        //    var target = Browser.Exists(By.Id("textbox-short"));
        //    var boundValue = Browser.Exists(By.Id("textbox-short-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-short-mirror"));
        //    Assert.Equal("-42", target.GetAttribute("value"));
        //    Assert.Equal("-42", boundValue.Text);
        //    Assert.Equal("-42", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to zero
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Leading zeros are not preserved
        //    target.SendKeys("42");
        //    Browser.Equal("042", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal("42", () => target.GetAttribute("value"));
        //    Assert.Equal("42", boundValue.Text);
        //    Assert.Equal("42", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableShort()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-short"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-short-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-short-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("-42\t");
        //    Browser.Equal("-42", () => boundValue.Text);
        //    Browser.Equal("-42", () => mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("42\t");
        //    Browser.Equal("42", () => boundValue.Text);
        //    Browser.Equal("42", () => mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxFloat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-float"));
        //    var boundValue = Browser.Exists(By.Id("textbox-float-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-float-mirror"));
        //    Assert.Equal("3.141", target.GetAttribute("value"));
        //    Assert.Equal("3.141", boundValue.Text);
        //    Assert.Equal("3.141", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to zero
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("-3.141\t");
        //    Browser.Equal("-3.141", () => target.GetAttribute("value"));
        //    Browser.Equal("-3.141", () => boundValue.Text);
        //    Browser.Equal("-3.141", () => mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableFloat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-float"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-float-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-float-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("3.141\t");
        //    Browser.Equal("3.141", () => boundValue.Text);
        //    Assert.Equal("3.141", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("-3.141\t");
        //    Browser.Equal("-3.141", () => boundValue.Text);
        //    Assert.Equal("-3.141", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxDouble()
        //{
        //    var target = Browser.Exists(By.Id("textbox-double"));
        //    var boundValue = Browser.Exists(By.Id("textbox-double-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-double-mirror"));
        //    Assert.Equal("3.14159265359", target.GetAttribute("value"));
        //    Assert.Equal("3.14159265359", boundValue.Text);
        //    Assert.Equal("3.14159265359", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to default
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("-3.14159265359\t");
        //    Browser.Equal("-3.14159265359", () => boundValue.Text);
        //    Assert.Equal("-3.14159265359", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Double shouldn't preserve trailing zeros
        //    target.Clear();
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("0.010\t");
        //    Browser.Equal("0.01", () => target.GetAttribute("value"));
        //    Assert.Equal("0.01", boundValue.Text);
        //    Assert.Equal("0.01", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableDouble()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-double"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-double-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-double-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("3.14159265359\t");
        //    Browser.Equal("3.14159265359", () => boundValue.Text);
        //    Assert.Equal("3.14159265359", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("-3.14159265359\t");
        //    Browser.Equal("-3.14159265359", () => boundValue.Text);
        //    Assert.Equal("-3.14159265359", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Double shouldn't preserve trailing zeros
        //    target.Clear();
        //    target.SendKeys("0.010\t");
        //    Browser.Equal("0.01", () => boundValue.Text);
        //    Assert.Equal("0.01", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxDecimal()
        //{
        //    var target = Browser.Exists(By.Id("textbox-decimal"));
        //    var boundValue = Browser.Exists(By.Id("textbox-decimal-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-decimal-mirror"));
        //    Assert.Equal("0.0000000000000000000000000001", target.GetAttribute("value"));
        //    Assert.Equal("0.0000000000000000000000000001", boundValue.Text);
        //    Assert.Equal("0.0000000000000000000000000001", mirrorValue.GetAttribute("value"));

        //    // Clear textbox; value updates to zero because that's the default
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Decimal should preserve trailing zeros
        //    target.SendKeys("0.010\t");
        //    Browser.Equal("0.010", () => boundValue.Text);
        //    Assert.Equal("0.010", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxNullableDecimal()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-decimal"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-decimal-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-decimal-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("0.0000000000000000000000000001\t");
        //    Browser.Equal("0.0000000000000000000000000001", () => boundValue.Text);
        //    Assert.Equal("0.0000000000000000000000000001", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // Decimal should preserve trailing zeros
        //    target.Clear();
        //    target.SendKeys("0.010\t");
        //    Browser.Equal("0.010", () => boundValue.Text);
        //    Browser.Equal("0.010", () => mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// This tests what happens you put invalid (unconvertable) input in. This is separate from the
        //// other tests because it requires type="text" - the other tests use type="number"
        //[Fact]
        //public void CanBindTextbox_Decimal_InvalidInput()
        //{
        //    var target = Browser.Exists(By.Id("textbox-decimal-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-decimal-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-decimal-invalid-mirror"));
        //    Assert.Equal("0.0000000000000000000000000001", target.GetAttribute("value"));
        //    Assert.Equal("0.0000000000000000000000000001", boundValue.Text);
        //    Assert.Equal("0.0000000000000000000000000001", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("0.01\t");
        //    Browser.Equal("0.01", () => boundValue.Text);
        //    Assert.Equal("0.01", mirrorValue.GetAttribute("value"));

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys("2A");
        //    Assert.Equal("0.012A", target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal("0.01", () => boundValue.Text);
        //    Browser.Equal("0.01", () => mirrorValue.GetAttribute("value"));
        //    Browser.Equal("0.01", () => target.GetAttribute("value"));

        //    // Continue editing with valid inputs
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("2\t");
        //    Browser.Equal("0.02", () => boundValue.Text);
        //    Browser.Equal("0.02", () => mirrorValue.GetAttribute("value"));
        //}

        //// This tests what happens you put invalid (unconvertable) input in. This is separate from the
        //// other tests because it requires type="text" - the other tests use type="number"
        //[Fact]
        //public void CanBindTextbox_NullableDecimal_InvalidInput()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-decimal-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-decimal-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-decimal-invalid-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("0.01\t");
        //    Browser.Equal("0.01", () => boundValue.Text);
        //    Assert.Equal("0.01", mirrorValue.GetAttribute("value"));

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys("2A");
        //    Assert.Equal("0.012A", target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal("0.01", () => boundValue.Text);
        //    Assert.Equal("0.01", mirrorValue.GetAttribute("value"));
        //    Assert.Equal("0.01", target.GetAttribute("value"));

        //    // Continue editing with valid inputs
        //    target.SendKeys(Keys.Backspace);
        //    target.SendKeys("2\t");
        //    Browser.Equal("0.02", () => boundValue.Text);
        //    Assert.Equal("0.02", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxGenericInt()
        //{
        //    var target = Browser.Exists(By.Id("textbox-generic-int"));
        //    var boundValue = Browser.Exists(By.Id("textbox-generic-int-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-generic-int-mirror"));
        //    Assert.Equal("-42", target.GetAttribute("value"));
        //    Assert.Equal("-42", boundValue.Text);
        //    Assert.Equal("-42", mirrorValue.GetAttribute("value"));

        //    // Clear target; value resets to zero
        //    target.Clear();
        //    Browser.Equal("0", () => target.GetAttribute("value"));
        //    Assert.Equal("0", boundValue.Text);
        //    Assert.Equal("0", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("42\t");
        //    Browser.Equal("42", () => boundValue.Text);
        //    Assert.Equal("42", mirrorValue.GetAttribute("value"));
        //}

        //[Fact]
        //public void CanBindTextboxGenericGuid()
        //{
        //    var target = Browser.Exists(By.Id("textbox-generic-guid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-generic-guid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-generic-guid-mirror"));
        //    Assert.Equal("00000000-0000-0000-0000-000000000000", target.GetAttribute("value"));
        //    Assert.Equal("00000000-0000-0000-0000-000000000000", boundValue.Text);
        //    Assert.Equal("00000000-0000-0000-0000-000000000000", mirrorValue.GetAttribute("value"));

        //    // Modify target; value is not updated because it's not convertable.
        //    target.Clear();
        //    Browser.Equal("00000000-0000-0000-0000-000000000000", () => boundValue.Text);
        //    Assert.Equal("00000000-0000-0000-0000-000000000000", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    var newValue = Guid.NewGuid().ToString();
        //    target.SendKeys(newValue + "\t");
        //    Browser.Equal(newValue, () => boundValue.Text);
        //    Assert.Equal(newValue, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTime()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetime"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetime-mirror"));
        //    var expected = new DateTime(1985, 3, 4);
        //    Assert.Equal(expected, DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 01/01/0001 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(expected, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01/02/2000 00:00:00\t");
        //    expected = new DateTime(2000, 1, 2);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxNullableDateTime()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetime"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetime-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    var expected = new DateTime(2000, 1, 2);
        //    target.SendKeys("01/02/2000 00:00:00\t");
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTimeOffset()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetimeoffset"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetimeoffset-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetimeoffset-mirror"));
        //    var expected = new DateTimeOffset(new DateTime(1985, 3, 4), TimeSpan.FromHours(8));
        //    Assert.Equal(expected, DateTimeOffset.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 01/01/0001 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(expected, () => DateTimeOffset.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01/02/2000 00:00:00 +08:00\t");
        //    expected = new DateTimeOffset(new DateTime(2000, 1, 2), TimeSpan.FromHours(8));
        //    Browser.Equal(expected, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxNullableDateTimeOffset()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetimeoffset"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("01/02/2000 00:00:00 +08:00" + "\t");
        //    var expected = new DateTimeOffset(new DateTime(2000, 1, 2), TimeSpan.FromHours(8));
        //    Browser.Equal(expected, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTimeWithFormat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetime-format"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetime-format-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetime-format-mirror"));
        //    var expected = new DateTime(1985, 3, 4);
        //    Assert.Equal("03-04", target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to the default
        //    target.Clear();
        //    target.SendKeys("\t");
        //    expected = default;
        //    Browser.Equal("01-01", () => target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01-02\t");
        //    expected = new DateTime(DateTime.Now.Year, 1, 2);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxNullableDateTimeWithFormat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetime-format"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetime-format-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetime-format-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("01-02\t");
        //    var expected = new DateTime(DateTime.Now.Year, 1, 2);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTimeOffsetWithFormat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetimeoffset-format"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetimeoffset-format-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetimeoffset-format-mirror"));
        //    var expected = new DateTimeOffset(new DateTime(1985, 3, 4), TimeSpan.FromHours(8));
        //    Assert.Equal("03-04", target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal("01-01", () => target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01-02\t");
        //    expected = new DateTimeOffset(new DateTime(DateTime.Now.Year, 1, 2), TimeSpan.FromHours(0));
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        ////
        //// Guess what! Client-side and server-side also understand timezones differently. So for now we're comparing
        //// the parsed output without consideration for the timezone
        //[Fact]
        //public void CanBindTextboxNullableDateTimeOffsetWithFormat()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetimeoffset"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.SendKeys("01-02" + "\t");
        //    var expected = new DateTimeOffset(new DateTime(DateTime.Now.Year, 1, 2), TimeSpan.FromHours(0));
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxNullableDateTime_InvalidValue()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetime-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetime-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetime-invalid-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    var expected = new DateTime(2000, 1, 2);
        //    target.SendKeys("01/02/2000 00:00:00\t");
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06X");
        //    Browser.Equal("05/06X", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal(expected, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Now change it to something valid
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06\t");
        //    expected = new DateTime(DateTime.Now.Year, 5, 6);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTimeOffset_InvalidValue()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetimeoffset-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetimeoffset-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetimeoffset-invalid-mirror"));
        //    var expected = new DateTimeOffset(new DateTime(1985, 3, 4), TimeSpan.FromHours(8));
        //    Assert.Equal(expected, DateTimeOffset.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    expected = new DateTime(2000, 1, 2);
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01/02/2000 00:00:00\t");
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06X");
        //    Browser.Equal("05/06X", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);

        //    // Now change it to something valid
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06\t");
        //    expected = new DateTime(DateTime.Now.Year, 5, 6);
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxDateTimeWithFormat_InvalidValue()
        //{
        //    var target = Browser.Exists(By.Id("textbox-datetime-format-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-datetime-format-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-datetime-format-invalid-mirror"));
        //    var expected = new DateTime(1985, 3, 4);
        //    Assert.Equal("03-04", target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06");
        //    Browser.Equal("05/06", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal("03-04", () => target.GetAttribute("value"));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Now change it to something valid
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05-06\t");
        //    expected = new DateTime(DateTime.Now.Year, 5, 6);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTextboxNullableDateTimeOffsetWithFormat_InvalidValue()
        //{
        //    var target = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-format-invalid"));
        //    var boundValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-format-invalid-value"));
        //    var mirrorValue = Browser.Exists(By.Id("textbox-nullable-datetimeoffset-format-invalid-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    var expected = new DateTimeOffset(new DateTime(DateTime.Now.Year, 1, 2));
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("01-02\t");
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);

        //    // Modify target to something invalid - the invalid change is reverted
        //    // back to the last valid value
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05/06");
        //    Browser.Equal("05/06", () => target.GetAttribute("value"));
        //    target.SendKeys("\t");
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);

        //    // Now change it to something valid
        //    target.SendKeys(Keys.Control + "a"); // select all
        //    target.SendKeys("05-06\t");
        //    expected = new DateTime(DateTime.Now.Year, 5, 6);
        //    Browser.Equal(expected.DateTime, () => DateTimeOffset.Parse(boundValue.Text, CultureInfo.InvariantCulture).DateTime);
        //    Assert.Equal(expected.DateTime, DateTimeOffset.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture).DateTime);
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindDateTimeLocalTextboxDateTime()
        //{
        //    var target = Browser.Exists(By.Id("datetime-local-textbox-datetime"));
        //    var boundValue = Browser.Exists(By.Id("datetime-local-textbox-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("datetime-local-textbox-datetime-mirror"));
        //    var expected = new DateTime(1985, 3, 4);
        //    Assert.Equal(expected, DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 01/01/0001 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(expected, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#datetime-local-textbox-datetime", "2000-01-02T04:05:06");
        //    expected = new DateTime(2000, 1, 2, 04, 05, 06);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindDateTimeLocalTextboxNullableDateTime()
        //{
        //    var target = Browser.Exists(By.Id("datetime-local-textbox-nullable-datetime"));
        //    var boundValue = Browser.Exists(By.Id("datetime-local-textbox-nullable-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("datetime-local-textbox-nullable-datetime-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#datetime-local-textbox-nullable-datetime", "2000-01-02T04:05:06");
        //    var expected = new DateTime(2000, 1, 2, 04, 05, 06);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindMonthTextboxDateTime()
        //{
        //    var target = Browser.Exists(By.Id("month-textbox-datetime"));
        //    var boundValue = Browser.Exists(By.Id("month-textbox-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("month-textbox-datetime-mirror"));
        //    var expected = new DateTime(1985, 3, 1);
        //    Assert.Equal(expected, DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    // When the value gets displayed the first time it gets truncated to the 1st day,
        //    // until there is no change the bound value doesn't get updated.
        //    Assert.Equal(expected.AddDays(3), DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected.AddDays(3), DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 01/01/0001 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(expected, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#month-textbox-datetime", "2000-02");
        //    expected = new DateTime(2000, 2, 1);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindMonthTextboxNullableDateTime()
        //{
        //    var target = Browser.Exists(By.Id("month-textbox-nullable-datetime"));
        //    var boundValue = Browser.Exists(By.Id("month-textbox-nullable-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("month-textbox-nullable-datetime-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#month-textbox-nullable-datetime", "2000-02");
        //    var expected = new DateTime(2000, 2, 1);
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTimeTextboxDateTime()
        //{
        //    var target = Browser.Exists(By.Id("time-textbox-datetime"));
        //    var boundValue = Browser.Exists(By.Id("time-textbox-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("time-textbox-datetime-mirror"));
        //    var expected = DateTime.Now.Date.AddHours(8).AddMinutes(5);
        //    Assert.Equal(expected, DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 00:00 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(DateTime.Now.Date, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(default, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(default, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#time-textbox-datetime", "04:05");
        //    expected = DateTime.Now.Date.Add(new TimeSpan(4, 5, 0));
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTimeTextboxNullableDateTime()
        //{
        //    var target = Browser.Exists(By.Id("time-textbox-nullable-datetime"));
        //    var boundValue = Browser.Exists(By.Id("time-textbox-nullable-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("time-textbox-nullable-datetime-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#time-textbox-nullable-datetime", "05:06");
        //    var expected = DateTime.Now.Date.Add(new TimeSpan(05, 06, 0));
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTimeStepTextboxDateTime()
        //{
        //    var target = Browser.Exists(By.Id("time-step-textbox-datetime"));
        //    var boundValue = Browser.Exists(By.Id("time-step-textbox-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("time-step-textbox-datetime-mirror"));
        //    var expected = DateTime.Now.Date.Add(new TimeSpan(8, 5, 30));
        //    Assert.Equal(expected, DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Clear textbox; value updates to 00:00 because that's the default
        //    target.Clear();
        //    expected = default;
        //    Browser.Equal(DateTime.Now.Date, () => DateTime.Parse(target.GetAttribute("value"), CultureInfo.InvariantCulture));
        //    Assert.Equal(default, DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(default, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#time-step-textbox-datetime", "04:05:06");
        //    expected = DateTime.Now.Date.Add(new TimeSpan(4, 5, 6));
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));
        //}

        //// For date comparisons, we parse (non-formatted) values to compare them. Client-side and server-side
        //// Blazor have different formatting behaviour by default.
        //[Fact]
        //public void CanBindTimeStepTextboxNullableDateTime()
        //{
        //    var target = Browser.Exists(By.Id("time-step-textbox-nullable-datetime"));
        //    var boundValue = Browser.Exists(By.Id("time-step-textbox-nullable-datetime-value"));
        //    var mirrorValue = Browser.Exists(By.Id("time-step-textbox-nullable-datetime-mirror"));
        //    Assert.Equal(string.Empty, target.GetAttribute("value"));
        //    Assert.Equal(string.Empty, boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    Browser.Equal("", () => boundValue.Text);
        //    Assert.Equal("", mirrorValue.GetAttribute("value"));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    // We have to do it this way because the browser gets in the way when sending keys to the input
        //    // element directly.
        //    ApplyInputValue("#time-step-textbox-nullable-datetime", "05:06");
        //    var expected = DateTime.Now.Date.Add(new TimeSpan(05, 06, 0));
        //    Browser.Equal(expected, () => DateTime.Parse(boundValue.Text, CultureInfo.InvariantCulture));
        //    Assert.Equal(expected, DateTime.Parse(mirrorValue.GetAttribute("value"), CultureInfo.InvariantCulture));

        //    // Modify target; verify value is updated and that textboxes linked to the same data are updated
        //    target.Clear();
        //    target.SendKeys("\t");
        //    Browser.Equal(string.Empty, () => boundValue.Text);
        //    Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        //}

        //// Applies an input through javascript to datetime-local/month/time controls.
        //private void ApplyInputValue(string cssSelector, string value)
        //{
        //    // It's very difficult to enter an invalid value into an <input type=date>, because
        //    // most combinations of keystrokes get normalized to something valid. Additionally,
        //    // using Selenium's SendKeys interacts unpredictably with this normalization logic,
        //    // most likely based on timings. As a workaround, use JS to apply the values. This
        //    // should only be used when strictly necessary, as it doesn't represent actual user
        //    // interaction as authentically as SendKeys in other cases.
        //    var javascript = (IJavaScriptExecutor)Browser;
        //    javascript.ExecuteScript(
        //        $"var elem = document.querySelector('{cssSelector}');"
        //        + $"elem.value = '{value}';"
        //        + "elem.dispatchEvent(new KeyboardEvent('change'));");
        //}
    }
}
