// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class BindTest : BasicTestAppTestBase
    {
        public BindTest(BrowserFixture browserFixture, DevHostServerFixture<Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            Navigate(ServerPathBase, noReload: true);
            MountTestComponent<BindCasesComponent>();
        }
        
        [Fact]
        public void CanBindTextbox_InitiallyBlank()
        {
            var target = Browser.FindElement(By.Id("textbox-initially-blank"));
            var boundValue = Browser.FindElement(By.Id("textbox-initially-blank-value"));
            Assert.Equal(string.Empty, target.GetAttribute("value"));
            Assert.Equal(string.Empty, boundValue.Text);

            // Modify target; verify value is updated
            target.SendKeys("Changed value");
            Assert.Equal(string.Empty, boundValue.Text); // Doesn't update until change event
            target.SendKeys("\t");
            Assert.Equal("Changed value", boundValue.Text);
        }

        [Fact]
        public void CanBindTextbox_InitiallyPopulated()
        {
            var target = Browser.FindElement(By.Id("textbox-initially-populated"));
            var boundValue = Browser.FindElement(By.Id("textbox-initially-populated-value"));
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
            Assert.False(target.Selected);
            Assert.Equal("False", boundValue.Text);

            // Modify target; verify value is updated
            target.Click();
            Assert.True(target.Selected);
            Assert.Equal("True", boundValue.Text);
        }

        [Fact]
        public void CanBindCheckbox_InitiallyChecked()
        {
            var target = Browser.FindElement(By.Id("checkbox-initially-checked"));
            var boundValue = Browser.FindElement(By.Id("checkbox-initially-checked-value"));
            Assert.True(target.Selected);
            Assert.Equal("True", boundValue.Text);

            // Modify target; verify value is updated
            target.Click();
            Assert.False(target.Selected);
            Assert.Equal("False", boundValue.Text);
        }
    }
}
