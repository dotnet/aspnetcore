// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class QuickGridFooterTemplateTest
{
    private static IServiceProvider BuildServiceProvider()
    {
        var moduleLoadCompletion = new TaskCompletionSource();
        var moduleImportStarted = new TaskCompletionSource();
        var testJsRuntime = new TestJsRuntime(moduleLoadCompletion, moduleImportStarted);
        return new ServiceCollection()
            .AddSingleton<IJSRuntime>(testJsRuntime)
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();
    }

    private static TestRenderer RenderGrid(IComponent component, out int componentId)
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);
        componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);
        return renderer;
    }

    // The grid markup (including the <tfoot>) is emitted into the render tree of the internal
    // Defer component, so we inspect that component's current frames to assert on real output.
    private static IEnumerable<RenderTreeFrame> GetGridFrames(TestRenderer renderer)
    {
        var deferComponentId = renderer.Batches
            .SelectMany(batch => batch.GetComponentFrames<Defer>())
            .Select(frame => frame.ComponentId)
            .Distinct()
            .Single();
        return renderer.GetCurrentRenderTreeFrames(deferComponentId).AsEnumerable();
    }

    private static bool HasElement(IEnumerable<RenderTreeFrame> frames, string elementName)
        => frames.Any(frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == elementName);

    private static int CountElements(IEnumerable<RenderTreeFrame> frames, string elementName)
        => frames.Count(frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == elementName);

    private static bool ContainsContent(IEnumerable<RenderTreeFrame> frames, string text)
        => frames.Any(frame =>
            (frame.FrameType == RenderTreeFrameType.Text || frame.FrameType == RenderTreeFrameType.Markup)
            && frame.TextContent is { } content
            && content.Contains(text, StringComparison.Ordinal));

    private static bool HasAttributeContaining(IEnumerable<RenderTreeFrame> frames, string attributeName, string value)
        => frames.Any(frame => frame.FrameType == RenderTreeFrameType.Attribute
            && frame.AttributeName == attributeName
            && frame.AttributeValue is string attributeValue
            && attributeValue.Contains(value, StringComparison.Ordinal));

    private static int CountAttributeContaining(IEnumerable<RenderTreeFrame> frames, string attributeName, string value)
        => frames.Count(frame => frame.FrameType == RenderTreeFrameType.Attribute
            && frame.AttributeName == attributeName
            && frame.AttributeValue is string attributeValue
            && attributeValue.Contains(value, StringComparison.Ordinal));

    [Fact]
    public void FooterTemplate_NotSet_DoesNotRenderTfoot()
    {
        var renderer = RenderGrid(new GridWithNoFooter(), out _);

        Assert.False(HasElement(GetGridFrames(renderer), "tfoot"));
    }

    [Fact]
    public void FooterTemplate_Set_RendersWithFooterContent()
    {
        var renderer = RenderGrid(new GridWithFooter(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Footer content"));
    }

    [Fact]
    public void FooterTemplate_Set_DoesNotAffectColumnCollection()
    {
        var renderer = RenderGrid(new GridWithFooterAndMultipleColumns(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Total: 2"));
        Assert.Equal(2, CountElements(frames, "th"));
    }

    [Fact]
    public void FooterTemplate_UpdatedOnRerender_ReflectsNewContent()
    {
        var component = new GridWithDynamicFooter();
        var renderer = RenderGrid(component, out var componentId);

        Assert.True(ContainsContent(GetGridFrames(renderer), "Initial footer"));

        component.FooterText = "Updated footer";
        renderer.RenderRootComponent(componentId);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Updated footer"));
        Assert.False(ContainsContent(frames, "Initial footer"));
    }

    [Fact]
    public void FooterTemplate_SetToNull_RemovesFooter()
    {
        var component = new GridWithToggleableFooter();
        var renderer = RenderGrid(component, out var componentId);

        Assert.True(HasElement(GetGridFrames(renderer), "tfoot"));

        component.ShowFooter = false;
        renderer.RenderRootComponent(componentId);

        Assert.False(HasElement(GetGridFrames(renderer), "tfoot"));
    }

    [Fact]
    public void ColumnFooterTemplate_NotSet_DoesNotRenderColumnFooterRow()
    {
        var renderer = RenderGrid(new GridWithNoColumnFooters(), out _);

        Assert.False(HasElement(GetGridFrames(renderer), "tfoot"));
    }

    [Fact]
    public void ColumnFooterTemplate_Set_RendersColumnFooterRow()
    {
        var renderer = RenderGrid(new GridWithColumnFooters(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Total IDs"));
        Assert.True(ContainsContent(frames, "Total Names"));
        Assert.Equal(2, CountElements(frames, "th"));
    }

    [Fact]
    public void ColumnFooterTemplate_PartialColumns_RendersFooterRowForAllColumns()
    {
        var renderer = RenderGrid(new GridWithPartialColumnFooters(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Count: 2"));
        Assert.Equal(2, CountAttributeContaining(frames, "class", "grid-column-footer-cell"));
    }

    [Fact]
    public void ColumnFooterTemplate_AndGridFooterTemplate_BothRender()
    {
        var renderer = RenderGrid(new GridWithBothFooterTypes(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Column footer"));
        Assert.True(ContainsContent(frames, "Grand total: 2"));
    }

    [Fact]
    public void FooterTemplate_WithEmptyItems_StillRenders()
    {
        var renderer = RenderGrid(new GridWithFooterAndEmptyItems(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "No items"));
    }

    [Fact]
    public void ColumnFooterTemplate_WithEmptyItems_StillRenders()
    {
        var renderer = RenderGrid(new GridWithColumnFootersAndEmptyItems(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Total: 0"));
    }

    [Fact]
    public void FooterTemplate_WithPagination_Renders()
    {
        var renderer = RenderGrid(new GridWithFooterAndPagination(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Page 1"));
    }

    [Fact]
    public void ColumnFooterTemplate_WithSortableColumn_Renders()
    {
        var renderer = RenderGrid(new GridWithColumnFooterOnSortableColumn(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "ID total"));
    }

    [Fact]
    public void FooterTemplate_WithRowClass_BothApplied()
    {
        var renderer = RenderGrid(new GridWithFooterAndRowClass(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Footer with row class"));
        Assert.True(HasAttributeContaining(frames, "class", "odd"));
        Assert.True(HasAttributeContaining(frames, "class", "even"));
    }

    [Fact]
    public void ColumnFooterTemplate_UpdatedOnRerender_ReflectsNewContent()
    {
        var component = new GridWithDynamicColumnFooter();
        var renderer = RenderGrid(component, out var componentId);

        Assert.True(ContainsContent(GetGridFrames(renderer), "Initial column footer"));

        component.ColumnFooterText = "Updated column footer";
        renderer.RenderRootComponent(componentId);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Updated column footer"));
        Assert.False(ContainsContent(frames, "Initial column footer"));
    }

    [Fact]
    public void FooterTemplate_WithItemsProvider_Renders()
    {
        var renderer = RenderGrid(new GridWithFooterAndItemsProvider(), out _);

        var frames = GetGridFrames(renderer);
        Assert.True(HasElement(frames, "tfoot"));
        Assert.True(ContainsContent(frames, "Provider footer"));
    }

    // To display sort-aware aggregates in a footer (e.g., "first item in current sort order"),
    // use ItemsProvider to capture the sorted items and update a variable the footer template reads.
    // The footer template is a RenderFragment — it re-renders when the grid refreshes after sort.
    [Fact]
    public async Task FooterTemplate_WithItemsProvider_ReflectsFirstItemAfterSort()
    {
        var component = new GridWithSortAwareFooterViaProvider();
        var renderer = RenderGrid(component, out _);

        // Initially unsorted: natural order, first item is ID=1
        Assert.Equal(1, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 1"));

        // Sort by ID descending: highest ID (3) should become first
        await renderer.Dispatcher.InvokeAsync(component.SortByIdDescendingAsync);
        Assert.Equal(3, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 3"));

        // Sort by ID ascending: lowest ID (1) should become first again
        await renderer.Dispatcher.InvokeAsync(component.SortByIdAscendingAsync);
        Assert.Equal(1, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 1"));
    }

    [Fact]
    public async Task ColumnFooterTemplate_WithItemsProvider_ReflectsFirstItemAfterSort()
    {
        var component = new GridWithSortAwareColumnFooterViaProvider();
        var renderer = RenderGrid(component, out _);

        // Initially unsorted: natural order, first item is ID=1
        Assert.Equal(1, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 1"));

        // Sort by ID descending: highest ID (3) should become first
        await renderer.Dispatcher.InvokeAsync(component.SortByIdDescendingAsync);
        Assert.Equal(3, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 3"));

        // Sort by ID ascending: lowest ID (1) should become first again
        await renderer.Dispatcher.InvokeAsync(component.SortByIdAscendingAsync);
        Assert.Equal(1, component.CurrentFirstItemId);
        Assert.True(ContainsContent(GetGridFrames(renderer), "First: 1"));
    }

    // --- Helper components ---

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static readonly IQueryable<TestItem> _testItems = new[]
    {
        new TestItem { Id = 1, Name = "Alice" },
        new TestItem { Id = 2, Name = "Bob" },
    }.AsQueryable();

    private sealed class GridWithNoFooter : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            RenderedWithFooter = false;
        }
    }

    private sealed class GridWithFooter : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, "Footer content");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
        }
    }

    private sealed class GridWithFooterAndMultipleColumns : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }
        public int ColumnCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();

                b.OpenComponent<PropertyColumn<TestItem, string>>(2);
                b.AddAttribute(3, "Property", (System.Linq.Expressions.Expression<Func<TestItem, string>>)(p => p.Name));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, $"Total: {_testItems.Count()}");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
            ColumnCount = 2;
        }
    }

    private sealed class GridWithDynamicFooter : ComponentBase
    {
        public string FooterText { get; set; } = "Initial footer";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, FooterText);
            }));
            builder.CloseComponent();
        }
    }

    private sealed class GridWithToggleableFooter : ComponentBase
    {
        public bool ShowFooter { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", ShowFooter
                ? (RenderFragment)(b => b.AddContent(0, "Footer content"))
                : null);
            builder.CloseComponent();
        }
    }

    private sealed class GridWithNoColumnFooters : ComponentBase
    {
        public bool HasColumnFooters { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            HasColumnFooters = false;
        }
    }

    private sealed class GridWithColumnFooters : ComponentBase
    {
        public bool HasColumnFooters { get; private set; }
        public int ColumnCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, "Total IDs")));
                b.CloseComponent();

                b.OpenComponent<PropertyColumn<TestItem, string>>(3);
                b.AddAttribute(4, "Property", (System.Linq.Expressions.Expression<Func<TestItem, string>>)(p => p.Name));
                b.AddAttribute(5, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, "Total Names")));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            HasColumnFooters = true;
            ColumnCount = 2;
        }
    }

    private sealed class GridWithPartialColumnFooters : ComponentBase
    {
        public bool HasColumnFooters { get; private set; }
        public int TotalColumns { get; private set; }
        public int ColumnsWithFooter { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();

                b.OpenComponent<PropertyColumn<TestItem, string>>(2);
                b.AddAttribute(3, "Property", (System.Linq.Expressions.Expression<Func<TestItem, string>>)(p => p.Name));
                b.AddAttribute(4, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, $"Count: {_testItems.Count()}")));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            HasColumnFooters = true;
            TotalColumns = 2;
            ColumnsWithFooter = 1;
        }
    }

    private sealed class GridWithBothFooterTypes : ComponentBase
    {
        public bool HasGridFooter { get; private set; }
        public bool HasColumnFooters { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, "Column footer")));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, $"Grand total: {_testItems.Count()}");
            }));
            builder.CloseComponent();

            HasGridFooter = true;
            HasColumnFooters = true;
        }
    }

    private sealed class GridWithFooterAndEmptyItems : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }
        public int ItemCount { get; private set; }

        private static readonly IQueryable<TestItem> _emptyItems = Array.Empty<TestItem>().AsQueryable();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _emptyItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, "No items");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
            ItemCount = 0;
        }
    }

    private sealed class GridWithColumnFootersAndEmptyItems : ComponentBase
    {
        public bool HasColumnFooters { get; private set; }
        public int ItemCount { get; private set; }

        private static readonly IQueryable<TestItem> _emptyItems = Array.Empty<TestItem>().AsQueryable();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _emptyItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, "Total: 0")));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            HasColumnFooters = true;
            ItemCount = 0;
        }
    }

    private sealed class GridWithFooterAndPagination : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }

        private readonly PaginationState _pagination = new() { ItemsPerPage = 1 };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "Pagination", _pagination);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(4, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, $"Page {_pagination.CurrentPageIndex + 1}");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
        }
    }

    private sealed class GridWithColumnFooterOnSortableColumn : ComponentBase
    {
        public bool HasColumnFooters { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "Sortable", true);
                b.AddAttribute(3, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, "ID total")));
                b.CloseComponent();

                b.OpenComponent<PropertyColumn<TestItem, string>>(4);
                b.AddAttribute(5, "Property", (System.Linq.Expressions.Expression<Func<TestItem, string>>)(p => p.Name));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            HasColumnFooters = true;
        }
    }

    private sealed class GridWithFooterAndRowClass : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }
        public bool RowClassApplied { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "RowClass", (Func<TestItem, string>)(item => item.Id % 2 == 0 ? "even" : "odd"));
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(4, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, "Footer with row class");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
            RowClassApplied = true;
        }
    }

    private sealed class GridWithDynamicColumnFooter : ComponentBase
    {
        public string ColumnFooterText { get; set; } = "Initial column footer";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "Items", _testItems);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "FooterTemplate", (RenderFragment)(fb => fb.AddContent(0, ColumnFooterText)));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class GridWithFooterAndItemsProvider : ComponentBase
    {
        public bool RenderedWithFooter { get; private set; }

        private static readonly GridItemsProvider<TestItem> _itemsProvider =
            request => ValueTask.FromResult(GridItemsProviderResult.From(
                new[] { new TestItem { Id = 1, Name = "Alice" } },
                totalItemCount: 1));

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "ItemsProvider", _itemsProvider);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, "Provider footer");
            }));
            builder.CloseComponent();

            RenderedWithFooter = true;
        }
    }

    // Captures the first item ID from each ItemsProvider call, so the grid-level FooterTemplate
    // can reflect the current sorted view.
    private sealed class GridWithSortAwareFooterViaProvider : ComponentBase
    {
        private static readonly TestItem[] _sourceItems =
        [
            new TestItem { Id = 1, Name = "Alice" },
            new TestItem { Id = 2, Name = "Bob" },
            new TestItem { Id = 3, Name = "Carol" },
        ];

        public int CurrentFirstItemId { get; private set; } = -1;

        private QuickGrid<TestItem> _grid = default!;
        private ColumnBase<TestItem> _idColumn = default!;
        private GridItemsProvider<TestItem> _provider;

        public Task SortByIdDescendingAsync() => _grid.SortByColumnAsync(_idColumn, SortDirection.Descending);
        public Task SortByIdAscendingAsync() => _grid.SortByColumnAsync(_idColumn, SortDirection.Ascending);

        private ValueTask<GridItemsProviderResult<TestItem>> ProvideItems(GridItemsProviderRequest<TestItem> request)
        {
            var sorted = request.ApplySorting(_sourceItems.AsQueryable()).ToArray();
            CurrentFirstItemId = sorted.Length > 0 ? sorted[0].Id : -1;
            return ValueTask.FromResult(GridItemsProviderResult.From(sorted, sorted.Length));
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var provider = _provider ??= ProvideItems;

            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "ItemsProvider", provider);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "Title", "Id");
                b.AddAttribute(3, "Sortable", true);
                b.AddComponentReferenceCapture(4, r => _idColumn = (ColumnBase<TestItem>)r);
                b.CloseComponent();
            }));
            builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
            {
                b.AddContent(0, $"First: {CurrentFirstItemId}");
            }));
            builder.AddComponentReferenceCapture(4, r => _grid = (QuickGrid<TestItem>)r);
            builder.CloseComponent();
        }
    }

    // Same pattern but with a column-level FooterTemplate instead of grid-level.
    private sealed class GridWithSortAwareColumnFooterViaProvider : ComponentBase
    {
        private static readonly TestItem[] _sourceItems =
        [
            new TestItem { Id = 1, Name = "Alice" },
            new TestItem { Id = 2, Name = "Bob" },
            new TestItem { Id = 3, Name = "Carol" },
        ];

        public int CurrentFirstItemId { get; private set; } = -1;

        private QuickGrid<TestItem> _grid = default!;
        private ColumnBase<TestItem> _idColumn = default!;
        private GridItemsProvider<TestItem> _provider;

        public Task SortByIdDescendingAsync() => _grid.SortByColumnAsync(_idColumn, SortDirection.Descending);
        public Task SortByIdAscendingAsync() => _grid.SortByColumnAsync(_idColumn, SortDirection.Ascending);

        private ValueTask<GridItemsProviderResult<TestItem>> ProvideItems(GridItemsProviderRequest<TestItem> request)
        {
            var sorted = request.ApplySorting(_sourceItems.AsQueryable()).ToArray();
            CurrentFirstItemId = sorted.Length > 0 ? sorted[0].Id : -1;
            return ValueTask.FromResult(GridItemsProviderResult.From(sorted, sorted.Length));
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var provider = _provider ??= ProvideItems;

            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "ItemsProvider", provider);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<TestItem, int>>(0);
                b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<TestItem, int>>)(p => p.Id));
                b.AddAttribute(2, "Title", "Id");
                b.AddAttribute(3, "Sortable", true);
                b.AddAttribute(4, "FooterTemplate", (RenderFragment)(fb =>
                {
                    fb.AddContent(0, $"First: {CurrentFirstItemId}");
                }));
                b.AddComponentReferenceCapture(5, r => _idColumn = (ColumnBase<TestItem>)r);
                b.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(3, r => _grid = (QuickGrid<TestItem>)r);
            builder.CloseComponent();
        }
    }
}
