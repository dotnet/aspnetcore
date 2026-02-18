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

    private int GetElementCount(By by) => Browser.FindElements(by).Count;

    private int GetElementCount(ISearchContext container, string cssSelector)
        => container.FindElements(By.CssSelector(cssSelector)).Count;

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
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

        // Scroll halfway.
        Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight * 0.5;");

        // Validate that we get the same item count after scrolling halfway.
        Browser.Equal(initialItemCount, GetItemCount);
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

        // Scroll to the bottom.
        Browser.ExecuteJavaScript("const container = document.getElementById('sync-container');container.scrollTop = container.scrollHeight;");

        // Validate that we get the same item count after scrolling to the bottom.
        Browser.Equal(initialItemCount, GetItemCount);
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

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
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

        Browser.ExecuteJavaScript("window.scrollTo(0, document.body.scrollHeight);");

        // Validate that the scroll event completed successfully
        Browser.True(() => Browser.Exists(By.Id("999")).Displayed);

        // Validate that the top spacer has expanded.
        Browser.NotEqual(expectedInitialSpacerStyle, () => topSpacer.GetDomAttribute("style"));
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));
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
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

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
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));

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
        Browser.True(() => topSpacer.GetDomAttribute("style").Contains(expectedInitialSpacerStyle));
        Assert.Contains("true", topSpacer.GetDomAttribute("aria-hidden"));
        Assert.Contains("true", bottomSpacer.GetDomAttribute("aria-hidden"));

        // Check scrolling document element works
        Browser.DoesNotExist(By.Id("row-999"));
        Browser.ExecuteJavaScript("window.scrollTo(0, document.body.scrollHeight);");
        Browser.True(() => Browser.Exists(By.Id("row-999")).Displayed);

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
        Browser.Equal(16, () => getItems().Count);
        Browser.Equal("Id: 0; Name: Thing 0", () => getItems().First().Text);

        // Scrolling still works and loads new data, though there's no guarantee about
        // exactly how many items will show up at any one time
        Browser.ExecuteJavaScript("document.getElementById('virtualize-scroll-area').scrollTop = 300;");
        Browser.NotEqual("Id: 0; Name: Thing 0", () => getItems().First().Text);
        Browser.True(() => getItems().Count > 3 && getItems().Count <= 16);
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

    [Fact]
    public void CanElevateEffectiveMaxItemCount_WhenOverscanExceedsMax()
    {
        Browser.MountTestComponent<VirtualizationLargeOverscan>();
        var container = Browser.Exists(By.Id("virtualize-large-overscan"));
        // Ensure we have an initial contiguous batch and the elevated effective max has kicked in (>= OverscanCount)
        var indices = GetVisibleItemIndices();
        Browser.True(() => indices.Count >= 200);

        // Give focus so PageDown works
        container.Click();

        var js = (IJavaScriptExecutor)Browser;
        var lastMaxIndex = -1;
        var lastScrollTop = -1L;

        // Check if we've reached (or effectively reached) the bottom
        var scrollHeight = (long)js.ExecuteScript("return arguments[0].scrollHeight", container);
        var clientHeight = (long)js.ExecuteScript("return arguments[0].clientHeight", container);
        var scrollTop = (long)js.ExecuteScript("return arguments[0].scrollTop", container);
        while (scrollTop + clientHeight < scrollHeight)
        {
            // Validate contiguity on the current page
            Browser.True(() => IsCurrentViewContiguous(indices));

            // Track progress in indices
            var currentMax = indices.Max();
            Assert.True(currentMax >= lastMaxIndex, $"Unexpected backward movement: previous max {lastMaxIndex}, current max {currentMax}.");
            lastMaxIndex = currentMax;

            // Send PageDown
            container.SendKeys(Keys.PageDown);

            // Wait for scrollTop to change (progress) to avoid infinite loop
            var prevScrollTop = scrollTop;
            Browser.True(() =>
            {
                var st = (long)js.ExecuteScript("return arguments[0].scrollTop", container);
                if (st > prevScrollTop)
                {
                    lastScrollTop = st;
                    return true;
                }
                return false;
            });
            scrollHeight = (long)js.ExecuteScript("return arguments[0].scrollHeight", container);
            clientHeight = (long)js.ExecuteScript("return arguments[0].clientHeight", container);
            scrollTop = (long)js.ExecuteScript("return arguments[0].scrollTop", container);
        }

        // Final contiguous assertion at bottom
        Browser.True(() => IsCurrentViewContiguous());

        // Helper: check visible items contiguous with no holes
        bool IsCurrentViewContiguous(List<int> existingIndices = null)
        {
            var indices = existingIndices ?? GetVisibleItemIndices();
            if (indices.Count == 0)
            {
                return false;
            }

            if (indices[^1] - indices[0] != indices.Count - 1)
            {
                return false;
            }
            for (var i = 1; i < indices.Count; i++)
            {
                if (indices[i] - indices[i - 1] != 1)
                {
                    return false;
                }
            }
            return true;
        }

        List<int> GetVisibleItemIndices()
        {
            var elements = container.FindElements(By.CssSelector(".large-overscan-item"));
            var list = new List<int>(elements.Count);
            foreach (var el in elements)
            {
                try
                {
                    var text = el.Text;
                    if (text.StartsWith("Item ", StringComparison.Ordinal))
                    {
                        if (int.TryParse(text.AsSpan(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                        {
                            list.Add(value);
                        }
                    }
                }
                catch (StaleElementReferenceException)
                {
                    // With variable-height support, item heights are measured and reported back to .NET,
                    // which recalculates spacer heights. This causes more frequent re-renders during scroll,
                    // and DOM elements can be replaced mid-iteration. Skipping stale elements is fine since
                    // the test collects indices across multiple scroll positions.
                }
            }
            return list;
        }
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

    /// <summary>
    /// Jumps to end using a single End key press, then waits for scroll position to stabilize.
    /// With the measurement-based scroll compensation in Virtualize, a single End press converges
    /// to the true bottom automatically. For async mode, handles loading data when placeholders appear.
    /// </summary>
    private void JumpToEndWithStabilization(
        IWebElement container,
        Func<bool> hasPlaceholders,
        Action loadData,
        int maxLoadRounds = 10)
    {
        var js = (IJavaScriptExecutor)Browser;

        // Ensure container has focus for keyboard input
        container.Click();

        // Single End key press — the scroll compensation in Virtualize should converge to the bottom
        container.SendKeys(Keys.End);

        // Handle async loading rounds: each time new items load, the scroll compensation may
        // trigger another data request. We only need to click "load" — not re-press End.
        for (int round = 0; round < maxLoadRounds; round++)
        {
            if (hasPlaceholders == null || !hasPlaceholders())
            {
                break;
            }

            loadData?.Invoke();
        }

        // Wait for scroll position to stabilize (compensation convergence)
        WaitForScrollStabilization(container);
    }

    /// <summary>
    /// Jumps to start using a single Home key press, then waits for scroll to reach the top.
    /// For async mode, handles loading data when placeholders appear.
    /// </summary>
    private void JumpToStartWithStabilization(
        IWebElement container,
        Func<bool> hasPlaceholders,
        Action loadData,
        Func<bool> isFirstItemVisible,
        int maxLoadRounds = 10)
    {
        var js = (IJavaScriptExecutor)Browser;

        // Ensure container has focus for keyboard input
        container.Click();

        // Single Home key press
        container.SendKeys(Keys.Home);

        // Wait for scroll to reach the top
        Browser.True(() => Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture) < 10);

        // Handle async loading rounds
        for (int round = 0; round < maxLoadRounds; round++)
        {
            if (hasPlaceholders == null || !hasPlaceholders())
            {
                break;
            }

            loadData?.Invoke();
        }

        // Wait for scroll position to stabilize
        WaitForScrollStabilization(container);
    }

    /// <summary>
    /// Waits for the scroll position to stop changing (stabilize within 1px across consecutive checks).
    /// </summary>
    private void WaitForScrollStabilization(IWebElement container)
    {
        var js = (IJavaScriptExecutor)Browser;
        double lastScrollTop = -1;
        Browser.True(() =>
        {
            var current = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture);
            if (Math.Abs(current - lastScrollTop) < 1 && lastScrollTop >= 0)
            {
                return true;
            }

            lastScrollTop = current;
            return false;
        });
    }

    [Theory]
    [InlineData(false)]  // sync
    [InlineData(true)]   // async
    public void VariableHeight_CanJumpToEndAndStart(bool useAsync)
    {
        IWebElement container;
        IWebElement finishLoadingButton = null;
        string itemClass, placeholderClass, firstItemId, lastItemId;

        if (useAsync)
        {
            Browser.MountTestComponent<VirtualizationVariableHeightAsync>();
            container = Browser.Exists(By.Id("async-variable-container"));
            finishLoadingButton = Browser.Exists(By.Id("finish-loading"));
            itemClass = ".async-variable-item";
            placeholderClass = ".async-variable-placeholder";
            firstItemId = "async-variable-item-0";
            lastItemId = "async-variable-item-999";

            // Load initial items
            finishLoadingButton.Click();
        }
        else
        {
            Browser.MountTestComponent<VirtualizationVariableHeight>();
            container = Browser.Exists(By.Id("variable-height-container"));
            itemClass = ".variable-height-item";
            placeholderClass = null; // sync mode has no placeholders
            firstItemId = "variable-item-0";
            lastItemId = "variable-item-999";
        }

        // Wait for initial items to appear
        Browser.True(() => GetElementCount(container, itemClass) > 0);

        // Jump to end using shared helper
        var hasPlaceholders = useAsync ? () => GetElementCount(container, placeholderClass) > 0 : (Func<bool>)null;
        var loadData = useAsync ? () => finishLoadingButton.Click() : (Action)null;
        JumpToEndWithStabilization(container, hasPlaceholders, loadData);

        // Wait for items to be rendered after loading
        Browser.True(() => GetElementCount(container, itemClass) > 0);

        // Verify last item is visible
        Browser.True(() => container.FindElements(By.Id(lastItemId)).Count > 0);

        // Jump back to start using shared helper
        JumpToStartWithStabilization(
            container,
            hasPlaceholders,
            loadData,
            () => container.FindElements(By.Id(firstItemId)).Count > 0);

        // Wait for items to render
        Browser.True(() => GetElementCount(container, itemClass) > 0);

        // Verify first item is visible
        Browser.True(() => container.FindElements(By.Id(firstItemId)).Count > 0);
    }

    [Fact]
    public void VariableHeight_ItemsRenderWithCorrectHeights()
    {
        Browser.MountTestComponent<VirtualizationVariableHeight>();

        var container = Browser.Exists(By.Id("variable-height-container"));

        // Wait for items to render
        Browser.True(() => GetElementCount(container, ".variable-height-item") > 0);

        // Check that item 0 has the expected height (20px from our test data: 20 + (0*37%1981) = 20)
        var item0 = container.FindElement(By.Id("variable-item-0"));
        var style0 = item0.GetDomAttribute("style");
        Assert.Contains("height: 20px", style0);

        // Check that item 1 has the expected height (57px from our test data: 20 + (1*37%1981) = 57)
        var item1 = container.FindElement(By.Id("variable-item-1"));
        var style1 = item1.GetDomAttribute("style");
        Assert.Contains("height: 57px", style1);
    }

    [Fact]
    public void DynamicContent_ItemHeightChangesUpdateLayout()
    {
        Browser.MountTestComponent<VirtualizationDynamicContent>();

        var container = Browser.Exists(By.Id("scroll-container"));
        var js = (IJavaScriptExecutor)Browser;
        var status = Browser.Exists(By.Id("status"));

        // Wait for items to render
        Browser.True(() => GetElementCount(container, ".item") > 0);

        // Get position of item 3 before expansion
        var item3TopBefore = container.FindElement(By.CssSelector("[data-index='3']")).Location.Y;

        // Expand item 2
        Browser.Exists(By.Id("expand-item-2")).Click();
        Browser.Equal("Item 2 expanded via button", () => status.Text);

        // Verify item 2 now has expanded content
        var item2 = container.FindElement(By.CssSelector("[data-index='2']"));
        Assert.Single(item2.FindElements(By.CssSelector(".expanded-content")));

        // Verify item 3 moved down (not overlapping with expanded item 2)
        var item3TopAfter = container.FindElement(By.CssSelector("[data-index='3']")).Location.Y;
        Assert.True(item3TopAfter > item3TopBefore,
            $"Item 3 should have moved down after item 2 expanded. Before: {item3TopBefore}, After: {item3TopAfter}");

        // Scroll down and back up to verify state is preserved
        js.ExecuteScript("arguments[0].scrollTop = 200", container);
        Browser.True(() => (long)js.ExecuteScript("return arguments[0].scrollTop", container) >= 200);

        js.ExecuteScript("arguments[0].scrollTop = 0", container);
        Browser.True(() => (long)js.ExecuteScript("return arguments[0].scrollTop", container) == 0);

        // Verify item 2 is still expanded after scrolling
        item2 = container.FindElement(By.CssSelector("[data-index='2']"));
        Assert.Single(item2.FindElements(By.CssSelector(".expanded-content")));
    }

    [Fact]
    public void DynamicContent_ExpandingOffScreenItemDoesNotAffectVisibleItems()
    {
        Browser.MountTestComponent<VirtualizationDynamicContent>();

        var container = Browser.Exists(By.Id("scroll-container"));
        var js = (IJavaScriptExecutor)Browser;
        var status = Browser.Exists(By.Id("status"));

        // Wait for items to render
        Browser.True(() => GetElementCount(container, ".item") > 0);

        // Scroll down so item 2 is not visible (items are 50px each, scroll past item 2)
        js.ExecuteScript("arguments[0].scrollTop = 200", container);
        Browser.True(() => (long)js.ExecuteScript("return arguments[0].scrollTop", container) >= 200);

        // Get the position of a visible item before expanding the off-screen item
        var visibleItems = container.FindElements(By.CssSelector(".item"));
        var firstVisibleItem = visibleItems.First();
        var firstVisibleTopBefore = firstVisibleItem.Location.Y;
        var firstVisibleIndex = firstVisibleItem.GetDomAttribute("data-index");

        // Expand item 2 (which should be above the visible area)
        Browser.Exists(By.Id("expand-item-2")).Click();
        Browser.Equal("Item 2 expanded via button", () => status.Text);

        // Verify the visible item position didn't change
        var sameItem = container.FindElement(By.CssSelector($"[data-index='{firstVisibleIndex}']"));
        var firstVisibleTopAfter = sameItem.Location.Y;

        // The visible items should stay in place (or very close, allowing for minor reflow)
        Assert.True(Math.Abs(firstVisibleTopAfter - firstVisibleTopBefore) < 5,
            $"Visible item should not have moved when off-screen item expanded. Before: {firstVisibleTopBefore}, After: {firstVisibleTopAfter}");
    }

    [Fact]
    public void VariableHeight_ContainerResizeWorks()
    {
        Browser.MountTestComponent<VirtualizationVariableHeight>();

        var container = Browser.Exists(By.Id("variable-height-container"));
        var resizeStatus = Browser.Exists(By.Id("resize-status"));
        var js = (IJavaScriptExecutor)Browser;

        // Wait for initial render at 100px
        Browser.True(() => GetElementCount(container, ".variable-height-item") > 0);

        // Resize to large (400px)
        Browser.Exists(By.Id("resize-large")).Click();
        Browser.Equal("Container resized to 400px", () => resizeStatus.Text);

        // Verify container resized
        var containerHeight = (long)js.ExecuteScript("return arguments[0].clientHeight", container);
        Assert.Equal(400, containerHeight);

        // Scroll to end and verify last item - use stabilization for large item counts
        JumpToEndWithStabilization(container, hasPlaceholders: null, loadData: null);
        Browser.True(() => container.FindElements(By.Id("variable-item-999")).Count > 0);

        // Resize to small while scrolled - should still work
        Browser.Exists(By.Id("resize-small")).Click();
        Browser.Equal("Container resized to 100px", () => resizeStatus.Text);
        containerHeight = (long)js.ExecuteScript("return arguments[0].clientHeight", container);
        Assert.Equal(100, containerHeight);

        // Scroll to top and verify first item - use stabilization
        JumpToStartWithStabilization(
            container,
            hasPlaceholders: null,
            loadData: null,
            () => container.FindElements(By.Id("variable-item-0")).Count > 0);
        Browser.True(() => container.FindElements(By.Id("variable-item-0")).Count > 0);
    }

    [Fact]
    public void VariableHeightAsync_LoadsItemsWithCorrectHeights()
    {
        Browser.MountTestComponent<VirtualizationVariableHeightAsync>();

        var container = Browser.Exists(By.Id("async-variable-container"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading"));

        // Initially, no items or placeholders (no data fetched yet - don't know totalItemCount)
        Browser.Equal(0, () => GetElementCount(container, ".async-variable-item"));
        Browser.Equal(0, () => GetElementCount(container, ".async-variable-placeholder"));

        // Finish loading
        finishLoadingButton.Click();

        // Items should appear
        Browser.True(() => GetElementCount(container, ".async-variable-item") > 0);

        // Verify first item has correct variable height (25 + (0 * 11 % 31) = 25px)
        var item0 = container.FindElement(By.Id("async-variable-item-0"));
        Assert.Contains("height: 25px", item0.GetDomAttribute("style"));

        // Verify second item has different height (25 + (1 * 11 % 31) = 36px)
        var item1 = container.FindElement(By.Id("async-variable-item-1"));
        Assert.Contains("height: 36px", item1.GetDomAttribute("style"));
    }

    [Fact]
    public void VariableHeightAsync_RtlLayoutWorks()
    {
        Browser.MountTestComponent<VirtualizationVariableHeightAsync>();

        var container = Browser.Exists(By.Id("async-variable-container"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading"));
        var js = (IJavaScriptExecutor)Browser;

        // Enable RTL
        Browser.Exists(By.Id("toggle-rtl")).Click();
        Browser.Equal("Direction: RTL", () => Browser.Exists(By.Id("direction-status")).Text);

        // Load initial items
        finishLoadingButton.Click();
        Browser.True(() => GetElementCount(container, ".async-variable-item") > 0);

        // Jump to end using stabilization helper (needed for large item counts with async loading)
        JumpToEndWithStabilization(
            container,
            () => GetElementCount(container, ".async-variable-placeholder") > 0,
            () => finishLoadingButton.Click());
        Browser.True(() => container.FindElements(By.Id("async-variable-item-999")).Count > 0);

        // Jump back to start using stabilization helper
        JumpToStartWithStabilization(
            container,
            () => GetElementCount(container, ".async-variable-placeholder") > 0,
            () => finishLoadingButton.Click(),
            () => container.FindElements(By.Id("async-variable-item-0")).Count > 0);
        Browser.True(() => container.FindElements(By.Id("async-variable-item-0")).Count > 0);
    }

    [Fact]
    public void VariableHeightAsync_CollectionMutationWorks()
    {
        Browser.MountTestComponent<VirtualizationVariableHeightAsync>();

        var container = Browser.Exists(By.Id("async-variable-container"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading"));
        var addItemStartButton = Browser.Exists(By.Id("add-item-start"));
        var removeItemMiddleButton = Browser.Exists(By.Id("remove-item-middle"));
        var refreshButton = Browser.Exists(By.Id("refresh-data"));
        var totalItemCount = Browser.Exists(By.Id("total-item-count"));
        var js = (IJavaScriptExecutor)Browser;

        // Load initial items
        finishLoadingButton.Click();
        Browser.True(() => GetElementCount(container, ".async-variable-item") > 0);
        Browser.Equal("Total: 1000", () => totalItemCount.Text);

        // Verify initial first item height (25 + 0*11%31 = 25px)
        var firstItem = container.FindElement(By.Id("async-variable-item-0"));
        Assert.Contains("height: 25px", firstItem.GetDomAttribute("style"));

        // Add item at START - this shifts ALL existing indices up
        // The new item has a distinctive 100px height
        addItemStartButton.Click();
        Browser.Equal("Total: 1001", () => totalItemCount.Text);

        // Refresh to see the change
        refreshButton.Click();
        finishLoadingButton.Click();

        // The new item 0 should have the distinctive 100px height
        firstItem = container.FindElement(By.Id("async-variable-item-0"));
        Assert.Contains("height: 100px", firstItem.GetDomAttribute("style"));

        // The old first item is now item 1 and should still have its original 25px height
        var secondItem = container.FindElement(By.Id("async-variable-item-1"));
        Assert.Contains("height: 25px", secondItem.GetDomAttribute("style"));

        // Remove item from MIDDLE - this shifts indices after the removed item
        removeItemMiddleButton.Click();
        Browser.Equal("Total: 1000", () => totalItemCount.Text);

        // Refresh
        refreshButton.Click();
        finishLoadingButton.Click();

        // Scroll to bottom and back to verify everything still works
        JumpToEndWithStabilization(
            container,
            () => GetElementCount(container, ".async-variable-placeholder") > 0,
            () => finishLoadingButton.Click());
        Browser.True(() => container.FindElements(By.Id("async-variable-item-999")).Count > 0);

        JumpToStartWithStabilization(
            container,
            () => GetElementCount(container, ".async-variable-placeholder") > 0,
            () => finishLoadingButton.Click(),
            () => container.FindElements(By.Id("async-variable-item-0")).Count > 0);

        // First item should still be the 100px tall item we added
        firstItem = container.FindElement(By.Id("async-variable-item-0"));
        Assert.Contains("height: 100px", firstItem.GetDomAttribute("style"));
    }

    [Fact]
    public void VariableHeightAsync_SmallItemCountsWork()
    {
        Browser.MountTestComponent<VirtualizationVariableHeightAsync>();

        var container = Browser.Exists(By.Id("async-variable-container"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading"));
        var setCount0Button = Browser.Exists(By.Id("set-count-0"));
        var setCount1Button = Browser.Exists(By.Id("set-count-1"));
        var setCount5Button = Browser.Exists(By.Id("set-count-5"));
        var refreshButton = Browser.Exists(By.Id("refresh-data"));
        var totalItemCount = Browser.Exists(By.Id("total-item-count"));

        // Load initial items
        finishLoadingButton.Click();
        Browser.True(() => GetElementCount(container, ".async-variable-item") > 0);
        Browser.Equal("Total: 1000", () => totalItemCount.Text);

        // Test empty list (0 items) - should show EmptyContent
        setCount0Button.Click();
        Browser.Equal("Total: 0", () => totalItemCount.Text);
        refreshButton.Click();
        finishLoadingButton.Click();
        Browser.Equal(0, () => GetElementCount(container, ".async-variable-item"));
        Browser.Exists(By.Id("no-data")); // EmptyContent should be visible

        // Test single item (1 item)
        setCount1Button.Click();
        Browser.Equal("Total: 1", () => totalItemCount.Text);
        refreshButton.Click();
        finishLoadingButton.Click();
        Browser.Equal(1, () => GetElementCount(container, ".async-variable-item"));
        Browser.DoesNotExist(By.Id("no-data")); // EmptyContent should NOT be visible
        var singleItem = container.FindElement(By.Id("async-variable-item-0"));
        Assert.Contains("height: 30px", singleItem.GetDomAttribute("style")); // 30 + 0*17%41 = 30px

        // Test 5 items - all should fit without virtualization
        setCount5Button.Click();
        Browser.Equal("Total: 5", () => totalItemCount.Text);
        refreshButton.Click();
        finishLoadingButton.Click();
        Browser.Equal(5, () => GetElementCount(container, ".async-variable-item"));
        Browser.DoesNotExist(By.Id("no-data"));

        // Verify all 5 items have variable heights (30 + i*17%41)
        var item0 = container.FindElement(By.Id("async-variable-item-0"));
        var item1 = container.FindElement(By.Id("async-variable-item-1"));
        var item2 = container.FindElement(By.Id("async-variable-item-2"));
        var item3 = container.FindElement(By.Id("async-variable-item-3"));
        var item4 = container.FindElement(By.Id("async-variable-item-4"));

        Assert.Contains("height: 30px", item0.GetDomAttribute("style")); // 30 + 0*17%41 = 30
        Assert.Contains("height: 47px", item1.GetDomAttribute("style")); // 30 + 1*17%41 = 47
        Assert.Contains("height: 64px", item2.GetDomAttribute("style")); // 30 + 2*34%41 = 64 (34%41=34)
        Assert.Contains("height: 40px", item3.GetDomAttribute("style")); // 30 + 3*51%41 = 30 + 10 = 40
        Assert.Contains("height: 57px", item4.GetDomAttribute("style")); // 30 + 4*68%41 = 30 + 27 = 57
    }

    [Theory]
    [InlineData(100, 100, 100)]  // baseline - no scaling
    [InlineData(50, 100, 100)]   // transform: scale(0.5)
    [InlineData(100, 50, 100)]   // CSS zoom: 0.5
    [InlineData(100, 100, 50)]   // CSS scale: 0.5
    [InlineData(200, 100, 100)]  // transform: scale(2)
    [InlineData(100, 200, 100)]  // CSS zoom: 2
    [InlineData(100, 100, 200)]  // CSS scale: 2
    [InlineData(75, 75, 75)]     // combined downscale: 0.75^3 ≈ 0.42x
    [InlineData(150, 150, 150)]  // combined upscale: 1.5^3 = 3.375x
    public void VariableHeightAsync_CanScrollWithoutFlashing(int transformScalePercent, int cssZoomPercent, int cssScalePercent)
    {
        Browser.MountTestComponent<VirtualizationVariableHeightAsync>();

        var container = Browser.Exists(By.Id("async-variable-container"));
        var finishLoadingButton = Browser.Exists(By.Id("finish-loading"));
        var js = (IJavaScriptExecutor)Browser;

        // Set transform scale if not 100%
        if (transformScalePercent != 100)
        {
            Browser.Exists(By.Id($"scale-{transformScalePercent}")).Click();
        }

        // Set CSS zoom if not 100%
        if (cssZoomPercent != 100)
        {
            Browser.Exists(By.Id($"zoom-{cssZoomPercent}")).Click();
        }

        // Set CSS scale if not 100%
        if (cssScalePercent != 100)
        {
            Browser.Exists(By.Id($"cssscale-{cssScalePercent}")).Click();
        }

        // Verify scale settings applied
        Browser.Equal($"Transform Scale: {transformScalePercent}%, CSS Zoom: {cssZoomPercent}%, CSS Scale: {cssScalePercent}%",
            () => Browser.Exists(By.Id("zoom-status")).Text);

        // Use 200 items for this test to keep test time reasonable across 9 scale combinations
        var setCount200Button = Browser.Exists(By.Id("set-count-200"));
        setCount200Button.Click();
        Browser.Exists(By.Id("refresh-data")).Click();

        // Load initial items
        finishLoadingButton.Click();
        Browser.True(() => GetElementCount(container, ".async-variable-item") > 0);

        // Scroll incrementally, checking for backward movement (flashing)
        int previousMinIndex = -1;
        int maxIndexSeen = -1;
        const int scrollIncrement = 100;
        const int maxIterations = 300;

        // JS to get min visible item index (avoids stale element issues)
        const string getMinIndexScript = @"
            const items = arguments[0].querySelectorAll('.async-variable-item');
            if (items.length === 0) return -1;
            let minIndex = Infinity;
            for (const item of items) {
                const match = item.id.match(/async-variable-item-(\d+)/);
                if (match) minIndex = Math.min(minIndex, parseInt(match[1], 10));
            }
            return minIndex === Infinity ? -1 : minIndex;";

        const string getMaxIndexScript = @"
            const items = arguments[0].querySelectorAll('.async-variable-item');
            if (items.length === 0) return -1;
            let maxIndex = -1;
            for (const item of items) {
                const match = item.id.match(/async-variable-item-(\d+)/);
                if (match) maxIndex = Math.max(maxIndex, parseInt(match[1], 10));
            }
            return maxIndex;";

        const string hasPlaceholdersScript = "return arguments[0].querySelectorAll('.async-variable-placeholder').length > 0;";

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            var scrollTopBefore = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture);

            js.ExecuteScript($"arguments[0].scrollTop += {scrollIncrement};", container);
            
            // Wait for scroll to actually change (deterministic instead of Thread.Sleep)
            Browser.True(() => Math.Abs(Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture) - scrollTopBefore) > 0.5
                || Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture) >= Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollHeight - arguments[0].clientHeight;", container), CultureInfo.InvariantCulture) - 1);

            // Handle async loading - wait for placeholders to be gone after clicking
            if ((bool)js.ExecuteScript(hasPlaceholdersScript, container))
            {
                finishLoadingButton.Click();
                Browser.True(() => Convert.ToInt32(js.ExecuteScript(getMinIndexScript, container), CultureInfo.InvariantCulture) >= 0);
            }

            var scrollTopAfter = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", container), CultureInfo.InvariantCulture);

            // Reached bottom?
            if (Math.Abs(scrollTopAfter - scrollTopBefore) < 1)
            {
                break;
            }

            var currentMinIndex = Convert.ToInt32(js.ExecuteScript(getMinIndexScript, container), CultureInfo.InvariantCulture);
            var currentMaxIndex = Convert.ToInt32(js.ExecuteScript(getMaxIndexScript, container), CultureInfo.InvariantCulture);

            if (currentMinIndex < 0)
            {
                continue;
            }

            // Check for flashing
            if (previousMinIndex != -1 && currentMinIndex < previousMinIndex)
            {
                Assert.Fail($"Flashing detected at iteration {iteration}: min index went from {previousMinIndex} to {currentMinIndex}");
            }

            previousMinIndex = currentMinIndex;
            maxIndexSeen = Math.Max(maxIndexSeen, currentMaxIndex);
        }

        // Verify we scrolled through items and reached near the end
        Assert.True(previousMinIndex > 0, "Should have scrolled past the first item");
        Assert.True(maxIndexSeen >= 199, $"Should have scrolled to the last item (saw up to index {maxIndexSeen})");
    }

    [Fact]
    public void DisplayModes_BlockLayout_SupportsVariableHeights()
    {
        Browser.MountTestComponent<VirtualizationDisplayModes>();

        var container = Browser.Exists(By.Id("block-container"));
        var itemCount = Browser.Exists(By.Id("block-count"));

        // Verify items are rendered
        Browser.Equal("50", () => itemCount.Text);
        Browser.True(() => GetElementCount(container, ".block-item") > 0);

        // Verify variable heights are applied (heights vary from 30-80px based on formula: 30 + i*17%51)
        var firstItem = container.FindElement(By.Id("block-item-0"));
        Assert.Contains("height: 30px", firstItem.GetDomAttribute("style")); // 30 + 0*17%51 = 30

        var secondItem = container.FindElement(By.Id("block-item-1"));
        Assert.Contains("height: 47px", secondItem.GetDomAttribute("style")); // 30 + 1*17%51 = 47

        // Scroll to bottom and verify virtualization works
        Browser.ExecuteJavaScript("document.getElementById('block-container').scrollTop = document.getElementById('block-container').scrollHeight;");
        Browser.True(() => GetElementCount(container, ".block-item") > 0);
    }

    [Fact]
    public void DisplayModes_GridLayout_SupportsVariableHeights()
    {
        Browser.MountTestComponent<VirtualizationDisplayModes>();

        var container = Browser.Exists(By.Id("grid-container"));
        var itemCount = Browser.Exists(By.Id("grid-count"));

        // Verify items are rendered
        Browser.Equal("50", () => itemCount.Text);
        Browser.True(() => GetElementCount(container, ".grid-item") > 0);

        // Verify variable heights are applied
        var firstItem = container.FindElement(By.Id("grid-item-0"));
        Assert.Contains("height: 30px", firstItem.GetDomAttribute("style"));

        // Scroll halfway and verify virtualization works
        Browser.ExecuteJavaScript("document.getElementById('grid-container').scrollTop = document.getElementById('grid-container').scrollHeight * 0.5;");
        Browser.True(() => GetElementCount(container, ".grid-item") > 0);

        // Scroll to bottom
        Browser.ExecuteJavaScript("document.getElementById('grid-container').scrollTop = document.getElementById('grid-container').scrollHeight;");
        Browser.True(() => GetElementCount(container, ".grid-item") > 0);
    }

    [Fact]
    public void DisplayModes_SubgridLayout_SupportsVariableHeights()
    {
        Browser.MountTestComponent<VirtualizationDisplayModes>();

        var container = Browser.Exists(By.Id("subgrid-container"));
        var itemCount = Browser.Exists(By.Id("subgrid-count"));

        // Verify items are rendered
        Browser.Equal("50", () => itemCount.Text);
        Browser.True(() => GetElementCount(container, ".subgrid-item") > 0);

        // Verify variable heights are applied
        var firstItem = container.FindElement(By.Id("subgrid-item-0"));
        Assert.Contains("height: 30px", firstItem.GetDomAttribute("style"));

        // Scroll and verify virtualization works with subgrid
        Browser.ExecuteJavaScript("document.getElementById('subgrid-container').scrollTop = document.getElementById('subgrid-container').scrollHeight;");
        Browser.True(() => GetElementCount(container, ".subgrid-item") > 0);
    }

    [Fact]
    public void QuickGrid_CanJumpToEndAndStart()
    {
        Browser.MountTestComponent<BasicTestApp.QuickGridTest.QuickGridVariableHeightComponent>();

        var container = Browser.Exists(By.Id("grid-variable-height"));
        var totalItems = Browser.Exists(By.Id("total-items"));
        var providerCallCount = Browser.Exists(By.Id("items-provider-call-count"));
        var dataLoaded = Browser.Exists(By.Id("data-loaded"));

        // QuickGrid-specific setup verification
        Browser.Equal("Total items: 1000", () => totalItems.Text);
        Browser.Equal("Data loaded: True", () => dataLoaded.Text);
        Browser.True(() => int.Parse(providerCallCount.Text.Replace("ItemsProvider calls: ", ""), CultureInfo.InvariantCulture) > 0);

        // Wait for any data rows to appear (not placeholders)
        WaitForQuickGridDataRows(container);

        // Helper to check if first row shows ID=1
        Func<bool> isFirstRowId1 = () =>
        {
            var rows = container.FindElements(By.CssSelector("tbody tr:not([aria-hidden])"));
            if (rows.Count == 0)
            {
                return false;
            }
            var firstCell = rows[0].FindElements(By.CssSelector("td:not(.grid-cell-placeholder)")).FirstOrDefault();
            return firstCell != null && firstCell.Text == "1";
        };

        // Jump to start and verify first item (ID=1) is visible
        JumpToStartWithStabilization(
            container,
            hasPlaceholders: null,  // QuickGrid handles its own async loading
            loadData: null,
            isFirstItemVisible: isFirstRowId1);

        // Final assertion for first row ID=1
        Browser.True(isFirstRowId1);

        // Jump to end using shared helper
        JumpToEndWithStabilization(container, hasPlaceholders: null, loadData: null);

        // Wait for data rows at bottom (QuickGrid-specific: no explicit load button)
        WaitForQuickGridDataRows(container);

        // Verify rows at bottom have data (IDs near 100)
        Browser.True(() =>
        {
            var rows = container.FindElements(By.CssSelector("tbody tr:not([aria-hidden])"));
            if (rows.Count == 0)
            {
                return false;
            }
            var firstDataCell = rows[0].FindElements(By.CssSelector("td:not(.grid-cell-placeholder)")).FirstOrDefault();
            return firstDataCell != null && int.TryParse(firstDataCell.Text, out var id) && id > 800;
        });

        // Jump back to start using shared helper
        JumpToStartWithStabilization(
            container,
            hasPlaceholders: null,
            loadData: null,
            isFirstItemVisible: isFirstRowId1);

        // Final assertion for first row ID=1 after scrolling back
        Browser.True(isFirstRowId1);
    }

    /// <summary>
    /// Waits for QuickGrid data rows to appear (not placeholders).
    /// </summary>
    private void WaitForQuickGridDataRows(IWebElement container)
    {
        Browser.True(() =>
        {
            var rows = container.FindElements(By.CssSelector("tbody tr:not([aria-hidden])"));
            if (rows.Count == 0)
            {
                return false;
            }
            var firstCell = rows[0].FindElements(By.CssSelector("td:not(.grid-cell-placeholder)")).FirstOrDefault();
            return firstCell != null && int.TryParse(firstCell.Text, out _);
        });
    }
}
