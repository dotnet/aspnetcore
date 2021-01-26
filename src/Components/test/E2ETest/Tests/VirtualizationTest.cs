// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class VirtualizationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public VirtualizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        [Fact]
        public void AlwaysFillsVisibleCapacity_Sync()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var topSpacer = Browser.Exists(By.Id("sync-container")).FindElement(By.TagName("div"));
            var expectedInitialSpacerStyle = "height: 0px;";

            int initialItemCount = 0;

            // Wait until items have been rendered.
            Browser.True(() => (initialItemCount = GetItemCount()) > 0);
            Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            // Scroll halfway.
            Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight * 0.5;");

            // Validate that we get the same item count after scrolling halfway.
            Browser.Equal(initialItemCount, GetItemCount);
            Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            // Scroll to the bottom.
            Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight;");

            // Validate that we get the same item count after scrolling to the bottom.
            Browser.Equal(initialItemCount, GetItemCount);
            Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            int GetItemCount() => Browser.FindElements(By.Id("sync-item")).Count;
        }

        [Fact]
        public void AlwaysFillsVisibleCapacity_Async()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var finishLoadingButton = Browser.Exists(By.Id("finish-loading-button"));

            // Check that no items or placeholders are visible.
            // No data fetches have happened so we don't know how many items there are.
            Browser.Equal(0, GetItemCount);
            Browser.Equal(0, GetPlaceholderCount);

            // Load the initial set of items.
            finishLoadingButton.Click();

            var initialItemCount = 0;

            // Validate that items appear and placeholders aren't rendered.
            Browser.True(() => (initialItemCount = GetItemCount()) > 0);
            Browser.Equal(0, GetPlaceholderCount);

            // Scroll halfway.
            Browser.ExecuteJavaScript("const container = document.getElementById('async-container');container.scrollTop = container.scrollHeight * 0.5;");

            // Validate that items are replaced by the same number of placeholders.
            Browser.Equal(0, GetItemCount);
            Browser.Equal(initialItemCount, GetPlaceholderCount);

            // Load the new set of items.
            finishLoadingButton.Click();

            // Validate that the placeholders are replaced by the same number of items.
            Browser.Equal(initialItemCount, GetItemCount);
            Browser.Equal(0, GetPlaceholderCount);

            // Scroll to the bottom.
            Browser.ExecuteJavaScript("const container = document.getElementById('async-container');container.scrollTop = container.scrollHeight;");

            // Validate that items are replaced by the same number of placeholders.
            Browser.Equal(0, GetItemCount);
            Browser.Equal(initialItemCount, GetPlaceholderCount);

            // Load the new set of items.
            finishLoadingButton.Click();

            // Validate that the placeholders are replaced by the same number of items.
            Browser.Equal(initialItemCount, GetItemCount);
            Browser.Equal(0, GetPlaceholderCount);

            int GetItemCount() => Browser.FindElements(By.Id("async-item")).Count;
            int GetPlaceholderCount() => Browser.FindElements(By.Id("async-placeholder")).Count;
        }

        [Fact]
        public void RerendersWhenItemSizeShrinks_Sync()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            int initialItemCount = 0;

            // Wait until items have been rendered.
            Browser.True(() => (initialItemCount = GetItemCount()) > 0);

            var itemSizeInput = Browser.Exists(By.Id("item-size-input"));

            // Change the item size.
            itemSizeInput.SendKeys("\b\b\b10\n");

            // Validate that the list has been re-rendered to show more items.
            Browser.True(() => GetItemCount() > initialItemCount);

            int GetItemCount() => Browser.FindElements(By.Id("sync-item")).Count;
        }

        [Fact]
        public void RerendersWhenItemSizeShrinks_Async()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var finishLoadingButton = Browser.Exists(By.Id("finish-loading-button"));

            // Load the initial set of items.
            finishLoadingButton.Click();

            int initialItemCount = 0;

            // Validate that items appear and placeholders aren't rendered.
            Browser.True(() => (initialItemCount = GetItemCount()) > 0);
            Browser.Equal(0, GetPlaceholderCount);

            var itemSizeInput = Browser.Exists(By.Id("item-size-input"));

            // Change the item size.
            itemSizeInput.SendKeys("\b\b\b10\n");

            // Validate that the same number of loaded items is rendered.
            Browser.Equal(initialItemCount, GetItemCount);
            Browser.True(() => GetPlaceholderCount() > 0);

            // Load the new set of items.
            finishLoadingButton.Click();

            // Validate that the placeholders have been replaced with more loaded items.
            Browser.True(() => GetItemCount() > initialItemCount);
            Browser.Equal(0, GetPlaceholderCount);

            int GetItemCount() => Browser.FindElements(By.Id("async-item")).Count;
            int GetPlaceholderCount() => Browser.FindElements(By.Id("async-placeholder")).Count;
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/25929")]
        public void CancelsOutdatedRefreshes_Async()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var cancellationCount = Browser.Exists(By.Id("cancellation-count"));
            var finishLoadingButton = Browser.Exists(By.Id("finish-loading-button"));

            // Load the initial set of items.
            finishLoadingButton.Click();

            // Validate that there are no initial cancellations.
            Browser.Equal("0", () => cancellationCount.Text);

            // Validate that there is no initial fetch to cancel.
            Browser.ExecuteJavaScript("const container = document.getElementById('async-container');container.scrollTop = 1000;");
            Browser.Equal("0", () => cancellationCount.Text);

            // Validate that scrolling again cancels the first fetch.
            Browser.ExecuteJavaScript("const container = document.getElementById('async-container');container.scrollTop = 2000;");
            Browser.Equal("1", () => cancellationCount.Text);

            // Validate that scrolling again cancels the second fetch.
            Browser.ExecuteJavaScript("const container = document.getElementById('async-container');container.scrollTop = 3000;");
            Browser.Equal("2", () => cancellationCount.Text);
        }

        [Fact]
        public void CanUseViewportAsContainer()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var expectedInitialSpacerStyle = "height: 0px;";
            var topSpacer = Browser.Exists(By.Id("viewport-as-root")).FindElement(By.TagName("div"));

            Browser.ExecuteJavaScript("const element = document.getElementById('viewport-as-root'); element.scrollIntoView();");

            // Validate that the top spacer has a height of zero.
            Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            Browser.ExecuteJavaScript("window.scrollTo(0, document.body.scrollHeight);");

            // Validate that the scroll event completed successfully
            var lastElement = Browser.Exists(By.Id("999"));
            Browser.True(() => lastElement.Displayed);

            // Validate that the top spacer has expanded.
            Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));
        }

        [Fact]
        public async Task ToleratesIncorrectItemSize()
        {
            Browser.MountTestComponent<VirtualizationComponent>();
            var topSpacer = Browser.Exists(By.Id("incorrect-size-container")).FindElement(By.TagName("div"));
            var expectedInitialSpacerStyle = "height: 0px;";

            // Wait until items have been rendered.
            Browser.True(() => GetItemCount() > 0);
            Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            // Scroll slowly, in increments of 50px at a time. At one point this would trigger a bug
            // due to the incorrect item size, whereby it would not realise it's necessary to show more
            // items because the first time the spacer became visible, the size calculation said that
            // we're already showing all the items we need to show.
            for (var pos = 0; pos < 1000; pos += 50)
            {
                Browser.ExecuteJavaScript($"document.getElementById('incorrect-size-container').scrollTop = {pos};");
                await Task.Delay(200);
            }

            // Validate that the top spacer did change
            Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetAttribute("style"));

            int GetItemCount() => Browser.FindElements(By.ClassName("incorrect-size-item")).Count;
        }

        [Fact]
        public void CanMutateDataInPlace_Sync()
        {
            Browser.MountTestComponent<VirtualizationDataChanges>();

            // Initial data
            var container = Browser.Exists(By.Id("using-items"));
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));

            // Mutate one of them
            var itemToMutate = container.FindElements(By.ClassName("person"))[1];
            itemToMutate.FindElement(By.TagName("button")).Click();

            // See changes
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2 MUTATED", name),
                name => Assert.Equal("Person 3", name));
        }

        [Fact]
        public void CanMutateDataInPlace_Async()
        {
            Browser.MountTestComponent<VirtualizationDataChanges>();

            // Initial data
            var container = Browser.Exists(By.Id("using-itemsprovider"));
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));

            // Mutate one of them
            var itemToMutate = container.FindElements(By.ClassName("person"))[1];
            itemToMutate.FindElement(By.TagName("button")).Click();

            // See changes
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2 MUTATED", name),
                name => Assert.Equal("Person 3", name));
        }

        [Fact]
        public void CanChangeDataCount_Sync()
        {
            Browser.MountTestComponent<VirtualizationDataChanges>();

            // Initial data
            var container = Browser.Exists(By.Id("using-items"));
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));

            // Add another item
            Browser.Exists(By.Id("add-person-to-fixed-list")).Click();

            // See changes
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name),
                name => Assert.Equal("Person 4", name));
        }

        [Fact]
        public void CanChangeDataCount_Async()
        {
            Browser.MountTestComponent<VirtualizationDataChanges>();

            // Initial data
            var container = Browser.Exists(By.Id("using-itemsprovider"));
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));

            // Add another item
            Browser.Exists(By.Id("add-person-to-itemsprovider")).Click();

            // Initially this has no effect because we don't re-query the provider until told to do so
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));

            // Request refresh
            Browser.Exists(By.Id("refresh-itemsprovider")).Click();

            // See changes
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name),
                name => Assert.Equal("Person 4", name));
        }

        [Fact]
        public void CanRefreshItemsProviderResultsInPlace()
        {
            Browser.MountTestComponent<VirtualizationDataChanges>();

            // Mutate the data
            var container = Browser.Exists(By.Id("using-itemsprovider"));
            var itemToMutate = container.FindElements(By.ClassName("person"))[1];
            itemToMutate.FindElement(By.TagName("button")).Click();

            // Verify the mutation was applied
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2 MUTATED", name),
                name => Assert.Equal("Person 3", name));

            // Refresh and verify the mutation was reverted
            Browser.Exists(By.Id("refresh-itemsprovider")).Click();
            Browser.Collection(() => GetPeopleNames(container),
                name => Assert.Equal("Person 1", name),
                name => Assert.Equal("Person 2", name),
                name => Assert.Equal("Person 3", name));
        }

        private string[] GetPeopleNames(IWebElement container)
        {
            var peopleElements = container.FindElements(By.CssSelector(".person span"));
            return peopleElements.Select(element => element.Text).ToArray();
        }
    }
}
