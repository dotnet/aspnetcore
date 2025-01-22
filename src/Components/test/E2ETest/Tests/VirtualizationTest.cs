// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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
        Navigate(ServerPathBase);
    }

    [Fact]
    public void AlwaysFillsVisibleCapacity_Sync()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        var topSpacer = Browser.Exists(By.Id("sync-container")).FindElement(By.TagName("div"));
        var expectedInitialSpacerStyle = "height: 0px; flex-shrink: 0;";

        int initialItemCount = 0;

        // Wait until items have been rendered.
        Browser.True(() => (initialItemCount = GetItemCount()) > 0);
        Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

        // Scroll halfway.
        Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight * 0.5;");

        // Validate that we get the same item count after scrolling halfway.
        Browser.Equal(initialItemCount, GetItemCount);
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

        // Scroll to the bottom.
        Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight;");

        // Validate that we get the same item count after scrolling to the bottom.
        Browser.Equal(initialItemCount, GetItemCount);
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

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
    public void CancelsOutdatedRefreshes_Async()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        var cancellationCount = Browser.Exists(By.Id("cancellation-count"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading-button"));
        var js = (IJavaScriptExecutor)Browser;

        // Load the initial set of items.
        finishLoadingButton.Click();

        // Validate that there are no initial cancellations.
        Browser.Equal("0", () => cancellationCount.Text);

        // Validate that scrolling causes cancellations
        for (var y = 1000; y <= 5000; y += 1000)
        {
            js.ExecuteScript($"document.getElementById('async-container').scrollTo({{ top: {y} }})");
            Browser.Equal(y, () => (long)js.ExecuteScript("return document.getElementById('async-container').scrollTop"));
        }

        Browser.True(() => int.Parse(cancellationCount.Text, CultureInfo.InvariantCulture) > 0);
    }

    [Fact]
    public void CanUseViewportAsContainer()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        var expectedInitialSpacerStyle = "height: 0px; flex-shrink: 0;";
        var topSpacer = Browser.Exists(By.Id("viewport-as-root")).FindElement(By.TagName("div"));

        Browser.ExecuteJavaScript("const element = document.getElementById('viewport-as-root'); element.scrollIntoView();");

        // Validate that the top spacer has a height of zero.
        Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

        Browser.ExecuteJavaScript("window.scrollTo(0, document.body.scrollHeight);");

        // Validate that the scroll event completed successfully
        var lastElement = Browser.Exists(By.Id("999"));
        Browser.True(() => lastElement.Displayed);

        // Validate that the top spacer has expanded.
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));
    }

    [Fact]
    public async Task ToleratesIncorrectItemSize()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        var topSpacer = Browser.Exists(By.Id("incorrect-size-container")).FindElement(By.TagName("div"));
        var expectedInitialSpacerStyle = "height: 0px; flex-shrink: 0;";

        // Wait until items have been rendered.
        Browser.True(() => GetItemCount() > 0);
        Browser.Equal(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

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
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));

        int GetItemCount() => Browser.FindElements(By.ClassName("incorrect-size-item")).Count;
    }

    [Fact]
    public void CanRenderHtmlTable()
    {
        Browser.MountTestComponent<VirtualizationTable>();
        var expectedInitialSpacerStyle = "height: 0px; flex-shrink: 0;";
        var topSpacer = Browser.Exists(By.CssSelector("#virtualized-table > tbody > :first-child"));
        var bottomSpacer = Browser.Exists(By.CssSelector("#virtualized-table > tbody > :last-child"));

        // We can override the tag name of the spacer
        Assert.Equal("tr", topSpacer.TagName.ToLowerInvariant());
        Assert.Equal("tr", bottomSpacer.TagName.ToLowerInvariant());
        Assert.Contains(expectedInitialSpacerStyle, topSpacer.GetDomAttribute("style"));

        // Check scrolling document element works
        Browser.DoesNotExist(By.Id("row-999"));
        Browser.ExecuteJavaScript("window.scrollTo(0, document.body.scrollHeight);");
        var lastElement = Browser.Exists(By.Id("row-999"));
        Browser.True(() => lastElement.Displayed);

        // Validate that the top spacer has expanded, and bottom one has collapsed
        Browser.False(() => topSpacer.GetDomAttribute("style").Contains(expectedInitialSpacerStyle));
        Assert.Contains(expectedInitialSpacerStyle, bottomSpacer.GetDomAttribute("style"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanLimitMaxItemsRendered(bool useAppContext)
    {
        if (useAppContext)
        {
            // This is to test back-compat with the switch added in a .NET 8 patch.
            // Newer applications shouldn't use this technique.
            Browser.MountTestComponent<VirtualizationMaxItemCount_AppContext>();
        }
        else
        {
            Browser.MountTestComponent<VirtualizationMaxItemCount>();
        }

        // Despite having a 600px tall scroll area and 30px high items (600/30=20),
        // we only render 10 items due to the MaxItemCount setting
        var scrollArea = Browser.Exists(By.Id("virtualize-scroll-area"));
        var getItems = () => scrollArea.FindElements(By.ClassName("my-item"));
        Browser.Equal(10, () => getItems().Count);
        Browser.Equal("Id: 0; Name: Thing 0", () => getItems().First().Text);

        // Scrolling still works and loads new data, though there's no guarantee about
        // exactly how many items will show up at any one time
        Browser.ExecuteJavaScript("document.getElementById('virtualize-scroll-area').scrollTop = 300;");
        Browser.NotEqual("Id: 0; Name: Thing 0", () => getItems().First().Text);
        Browser.True(() => getItems().Count > 3 && getItems().Count <= 10);
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

    [Theory]
    [InlineData("simple-scroll-horizontal")]
    [InlineData("complex-scroll-horizontal")]
    [InlineData("simple-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-tbody")]
    [InlineData("simple-display-table-scroll-horizontal")]
    [InlineData("complex-display-table-scroll-horizontal")]
    public void CanLoadNewDataWithHorizontalScrollToRight(string containerId)
    {
        Browser.MountTestComponent<VirtualizationDataChanges>();
        var dataSetLengthSelector = new SelectElement(Browser.Exists(By.Id("large-dataset-length")));
        var dataSetLengthLastRendered = () => int.Parse(Browser.FindElement(By.Id("large-dataset-length-lastrendered")).Text, CultureInfo.InvariantCulture);
        var container = Browser.Exists(By.Id(containerId));

        // Scroll to the end of a medium list
        dataSetLengthSelector.SelectByText("1000");
        Browser.Equal(1000, dataSetLengthLastRendered);
        Browser.True(() =>
        {
            ScrollLeftToEnd(Browser, container);
            ScrollTopToEnd(Browser, container);
            return GetPeopleNames(container).Contains("Person 1000");
        });

        Browser.True(() =>
        {
            ScrollLeftToEnd(Browser, container);
            ScrollTopToBeginning(Browser, container);
            return GetPeopleNames(container).Contains("Person 1");
        });
    }

    [Theory]
    [InlineData("simple-scroll-horizontal")]
    [InlineData("complex-scroll-horizontal")]
    [InlineData("simple-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-tbody")]
    [InlineData("simple-display-table-scroll-horizontal")]
    [InlineData("complex-display-table-scroll-horizontal")]
    [InlineData("removing-many")]
    public void CanExpandDataSetAndRetainScrollPosition(string containerId)
    {
        Browser.MountTestComponent<VirtualizationDataChanges>();
        var dataSetLengthSelector = new SelectElement(Browser.Exists(By.Id("large-dataset-length")));
        var dataSetLengthLastRendered = () => int.Parse(Browser.FindElement(By.Id("large-dataset-length-lastrendered")).Text, CultureInfo.InvariantCulture);
        var container = Browser.Exists(By.Id(containerId));

        // Scroll to the end of a medium list
        dataSetLengthSelector.SelectByText("1000");
        Browser.Equal(1000, dataSetLengthLastRendered);
        Browser.True(() =>
        {
            ScrollTopToEnd(Browser, container);
            return GetPeopleNames(container).Contains("Person 1000");
        });

        // Expand the data set
        dataSetLengthSelector.SelectByText("100000");
        Browser.Equal(100000, dataSetLengthLastRendered);

        // See that the old data is still visible, because the scroll position is preserved as a pixel count,
        // not a scroll percentage
        Browser.True(() => GetPeopleNames(container).Contains("Person 1000"));
    }

    [Theory]
    [InlineData("simple-scroll-horizontal")]
    [InlineData("complex-scroll-horizontal")]
    [InlineData("simple-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-parent")]
    [InlineData("complex-scroll-horizontal-on-tbody")]
    [InlineData("simple-display-table-scroll-horizontal")]
    [InlineData("complex-display-table-scroll-horizontal")]
    [InlineData("removing-many")]
    public void CanHandleDataSetShrinkingWithExistingOffsetAlreadyBeyondNewListEnd(string containerId)
    {
        // Represents https://github.com/dotnet/aspnetcore/issues/37245
        Browser.MountTestComponent<VirtualizationDataChanges>();
        var dataSetLengthSelector = new SelectElement(Browser.Exists(By.Id("large-dataset-length")));
        var dataSetLengthLastRendered = () => int.Parse(Browser.FindElement(By.Id("large-dataset-length-lastrendered")).Text, CultureInfo.InvariantCulture);
        var container = Browser.Exists(By.Id(containerId));

        // Scroll to the end of a very long list
        dataSetLengthSelector.SelectByText("100000");
        Browser.Equal(100000, dataSetLengthLastRendered);
        Browser.True(() =>
        {
            ScrollTopToEnd(Browser, container);
            return GetPeopleNames(container).Contains("Person 100000");
        });

        // Now make the dataset much shorter
        // We should automatically have the scroll position reduced to the new maximum
        // Because the new data set is *so much* shorter than the previous one, if bug #37245 were still here,
        // this would take over 30 minutes so the test would fail
        dataSetLengthSelector.SelectByText("25");
        Browser.Equal(25, dataSetLengthLastRendered);
        Browser.True(() => GetPeopleNames(container).Contains("Person 25"));
    }

    [Fact]
    public void EmptyContentRendered_Sync()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        Browser.Exists(By.Id("no-data-sync"));
    }

    [Fact]
    public void EmptyContentRendered_Async()
    {
        Browser.MountTestComponent<VirtualizationComponent>();
        var finishLoadingWithItemsButton = Browser.Exists(By.Id("finish-loading-button"));
        var finishLoadingWithoutItemsButton = Browser.Exists(By.Id("finish-loading-button-empty"));
        var refreshDataAsync = Browser.Exists(By.Id("refresh-data-async"));

        // Check that no items or placeholders are visible.
        // No data fetches have happened so we don't know how many items there are.
        Browser.Equal(0, GetItemCount);
        Browser.Equal(0, GetPlaceholderCount);

        // Check that <EmptyContent> is not shown while loading
        Browser.DoesNotExist(By.Id("no-data-async"));

        // Load the initial set of items.
        finishLoadingWithItemsButton.Click();

        // Check that <EmptyContent> is still not shown (because there are items loaded)
        Browser.DoesNotExist(By.Id("no-data-async"));

        // Start loading
        refreshDataAsync.Click();

        // Check that <EmptyContent> is not shown
        Browser.DoesNotExist(By.Id("no-data-async"));

        // Simulate 0 items
        finishLoadingWithoutItemsButton.Click();

        // Check that <EmptyContent> is shown
        Browser.Exists(By.Id("no-data-async"));

        int GetItemCount() => Browser.FindElements(By.Id("async-item")).Count;
        int GetPlaceholderCount() => Browser.FindElements(By.Id("async-placeholder")).Count;
    }

    private string[] GetPeopleNames(IWebElement container)
    {
        var peopleElements = container.FindElements(By.CssSelector(".person span"));
        return peopleElements.Select(element => element.Text).ToArray();
    }

    private static void ScrollTopToEnd(IWebDriver browser, IWebElement elem)
    {
        var js = (IJavaScriptExecutor)browser;
        js.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", elem);
    }

    private static void ScrollTopToBeginning(IWebDriver browser, IWebElement elem)
    {
        var js = (IJavaScriptExecutor)browser;
        js.ExecuteScript("arguments[0].scrollTop = 0", elem);
    }

    private static void ScrollLeftToEnd(IWebDriver Browser, IWebElement elem)
    {
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript("arguments[0].scrollLeft = arguments[0].scrollWidth", elem);
    }
}
