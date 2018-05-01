// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class BindTest : BasicTestAppTestBase
    {
        public BindTest(
            BrowserFixture browserFixture, 
            DevHostServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: true);
            MountTestComponent<BindCasesComponent>();
        }
        
        [Fact]
        public void CanBindTextbox_InitiallyBlank()
        {
            var target = Browser.FindElement(By.Id("textbox-initially-blank"));
            var boundValue = Browser.FindElement(By.Id("textbox-initially-blank-value"));
            var mirrorValue = Browser.FindElement(By.Id("textbox-initially-blank-mirror"));
            var setNullButton = Browser.FindElement(By.Id("textbox-initially-blank-setnull"));
            Assert.Equal(string.Empty, target.GetAttribute("value"));
            Assert.Equal(string.Empty, boundValue.Text);
            Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));

            // Modify target; verify value is updated and that textboxes linked to the same data are updated
            target.SendKeys("Changed value");
            Assert.Equal(string.Empty, boundValue.Text); // Doesn't update until change event
            Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
            target.SendKeys("\t");
            Assert.Equal("Changed value", boundValue.Text);
            Assert.Equal("Changed value", mirrorValue.GetAttribute("value"));

            // Remove the value altogether
            setNullButton.Click();
            Assert.Equal(string.Empty, target.GetAttribute("value"));
            Assert.Equal(string.Empty, boundValue.Text);
            Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        }

        [Fact]
        public void CanBindTextbox_InitiallyPopulated()
        {
            var target = Browser.FindElement(By.Id("textbox-initially-populated"));
            var boundValue = Browser.FindElement(By.Id("textbox-initially-populated-value"));
            var mirrorValue = Browser.FindElement(By.Id("textbox-initially-populated-mirror"));
            var setNullButton = Browser.FindElement(By.Id("textbox-initially-populated-setnull"));
            Assert.Equal("Hello", target.GetAttribute("value"));
            Assert.Equal("Hello", boundValue.Text);
            Assert.Equal("Hello", mirrorValue.GetAttribute("value"));

            // Modify target; verify value is updated and that textboxes linked to the same data are updated
            target.Clear();
            target.SendKeys("Changed value\t");
            Assert.Equal("Changed value", boundValue.Text);
            Assert.Equal("Changed value", mirrorValue.GetAttribute("value"));

            // Remove the value altogether
            setNullButton.Click();
            Assert.Equal(string.Empty, target.GetAttribute("value"));
            Assert.Equal(string.Empty, boundValue.Text);
            Assert.Equal(string.Empty, mirrorValue.GetAttribute("value"));
        }
        
        [Fact]
        public void CanBindTextArea_InitiallyBlank()
        {
            var target = Browser.FindElement(By.Id("textarea-initially-blank"));
            var boundValue = Browser.FindElement(By.Id("textarea-initially-blank-value"));
            Assert.Equal(string.Empty, target.GetAttribute("value"));
            Assert.Equal(string.Empty, boundValue.Text);

            // Modify target; verify value is updated
            target.SendKeys("Changed value");
            Assert.Equal(string.Empty, boundValue.Text); // Don't update as there's no change event fired yet.
            target.SendKeys("\t");
            Assert.Equal("Changed value", boundValue.Text);
        }

        [Fact]
        public void CanBindTextArea_InitiallyPopulated()
        {
            var target = Browser.FindElement(By.Id("textarea-initially-populated"));
            var boundValue = Browser.FindElement(By.Id("textarea-initially-populated-value"));
            Assert.Equal("Hello", target.GetAttribute("value"));
            Assert.Equal("Hello", boundValue.Text);

            // Modify target; verify value is updated
            target.Clear();
            target.SendKeys("Changed value\t");
            Assert.Equal("Changed value", boundValue.Text);
        }

        [Fact]
        public void CanBindCheckbox_InitiallyUnchecked()
        {
            var target = Browser.FindElement(By.Id("checkbox-initially-unchecked"));
            var boundValue = Browser.FindElement(By.Id("checkbox-initially-unchecked-value"));
            var invertButton = Browser.FindElement(By.Id("checkbox-initially-unchecked-invert"));
            Assert.False(target.Selected);
            Assert.Equal("False", boundValue.Text);

            // Modify target; verify value is updated
            target.Click();
            Assert.True(target.Selected);
            Assert.Equal("True", boundValue.Text);

            // Modify data; verify checkbox is updated
            invertButton.Click();
            Assert.False(target.Selected);
            Assert.Equal("False", boundValue.Text);
        }

        [Fact]
        public void CanBindCheckbox_InitiallyChecked()
        {
            var target = Browser.FindElement(By.Id("checkbox-initially-checked"));
            var boundValue = Browser.FindElement(By.Id("checkbox-initially-checked-value"));
            var invertButton = Browser.FindElement(By.Id("checkbox-initially-checked-invert"));
            Assert.True(target.Selected);
            Assert.Equal("True", boundValue.Text);

            // Modify target; verify value is updated
            target.Click();
            Assert.False(target.Selected);
            Assert.Equal("False", boundValue.Text);

            // Modify data; verify checkbox is updated
            invertButton.Click();
            Assert.True(target.Selected);
            Assert.Equal("True", boundValue.Text);
        }

        [Fact]
        public void CanBindSelect()
        {
            var target = new SelectElement(Browser.FindElement(By.Id("select-box")));
            var boundValue = Browser.FindElement(By.Id("select-box-value"));
            Assert.Equal("Second choice", target.SelectedOption.Text);
            Assert.Equal("Second", boundValue.Text);

            // Modify target; verify value is updated
            target.SelectByText("Third choice");
            Assert.Equal("Third", boundValue.Text);

            // Also verify we can add and select new options atomically
            // Don't move this into a separate test, because then the previous assertions
            // would be dependent on test execution order (or would require a full page reload)
            Browser.FindElement(By.Id("select-box-add-option")).Click();
            Assert.Equal("Fourth", boundValue.Text);
            Assert.Equal("Fourth choice", target.SelectedOption.Text);
        }
    }
}
