// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
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

    [Fact]
    public void FooterTemplate_NotSet_DoesNotRenderTfoot()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithNoFooter();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.False(component.RenderedWithFooter);
    }

    [Fact]
    public void FooterTemplate_Set_RendersWithFooterContent()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooter();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
    }

    [Fact]
    public void FooterTemplate_Set_DoesNotAffectColumnCollection()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooterAndMultipleColumns();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
        Assert.Equal(2, component.ColumnCount);
    }

    [Fact]
    public void FooterTemplate_UpdatedOnRerender_ReflectsNewContent()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithDynamicFooter();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.Equal("Initial footer", component.FooterText);

        component.FooterText = "Updated footer";
        renderer.RenderRootComponent(componentId);

        Assert.Equal("Updated footer", component.FooterText);
    }

    [Fact]
    public void FooterTemplate_SetToNull_RemovesFooter()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithToggleableFooter();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.ShowFooter);

        component.ShowFooter = false;
        renderer.RenderRootComponent(componentId);

        Assert.False(component.ShowFooter);
    }

    [Fact]
    public void ColumnFooterTemplate_NotSet_DoesNotRenderColumnFooterRow()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithNoColumnFooters();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.False(component.HasColumnFooters);
    }

    [Fact]
    public void ColumnFooterTemplate_Set_RendersColumnFooterRow()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithColumnFooters();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.HasColumnFooters);
        Assert.Equal(2, component.ColumnCount);
    }

    [Fact]
    public void ColumnFooterTemplate_PartialColumns_RendersFooterRowForAllColumns()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithPartialColumnFooters();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.HasColumnFooters);
        Assert.Equal(2, component.TotalColumns);
        Assert.Equal(1, component.ColumnsWithFooter);
    }

    [Fact]
    public void ColumnFooterTemplate_AndGridFooterTemplate_BothRender()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithBothFooterTypes();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.HasGridFooter);
        Assert.True(component.HasColumnFooters);
    }

    [Fact]
    public void FooterTemplate_WithEmptyItems_StillRenders()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooterAndEmptyItems();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
        Assert.Equal(0, component.ItemCount);
    }

    [Fact]
    public void ColumnFooterTemplate_WithEmptyItems_StillRenders()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithColumnFootersAndEmptyItems();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.HasColumnFooters);
        Assert.Equal(0, component.ItemCount);
    }

    [Fact]
    public void FooterTemplate_WithPagination_Renders()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooterAndPagination();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
    }

    [Fact]
    public void ColumnFooterTemplate_WithSortableColumn_Renders()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithColumnFooterOnSortableColumn();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.HasColumnFooters);
    }

    [Fact]
    public void FooterTemplate_WithRowClass_BothApplied()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooterAndRowClass();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
        Assert.True(component.RowClassApplied);
    }

    [Fact]
    public void ColumnFooterTemplate_UpdatedOnRerender_ReflectsNewContent()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithDynamicColumnFooter();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.Equal("Initial column footer", component.ColumnFooterText);

        component.ColumnFooterText = "Updated column footer";
        renderer.RenderRootComponent(componentId);

        Assert.Equal("Updated column footer", component.ColumnFooterText);
    }

    [Fact]
    public void FooterTemplate_WithItemsProvider_Renders()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithFooterAndItemsProvider();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        Assert.True(component.RenderedWithFooter);
    }

    // To display sort-aware aggregates in a footer (e.g., "first item in current sort order"),
    // use ItemsProvider to capture the sorted items and update a variable the footer template reads.
    // The footer template is a RenderFragment — it re-renders when the grid refreshes after sort.
    [Fact]
    public async Task FooterTemplate_WithItemsProvider_ReflectsFirstItemAfterSort()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithSortAwareFooterViaProvider();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Initially unsorted: natural order, first item is ID=1
        Assert.Equal(1, component.CurrentFirstItemId);

        // Sort by ID descending: highest ID (3) should become first
        await renderer.Dispatcher.InvokeAsync(component.SortByIdDescendingAsync);
        Assert.Equal(3, component.CurrentFirstItemId);

        // Sort by ID ascending: lowest ID (1) should become first again
        await renderer.Dispatcher.InvokeAsync(component.SortByIdAscendingAsync);
        Assert.Equal(1, component.CurrentFirstItemId);
    }

    [Fact]
    public async Task ColumnFooterTemplate_WithItemsProvider_ReflectsFirstItemAfterSort()
    {
        var serviceProvider = BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var component = new GridWithSortAwareColumnFooterViaProvider();
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Initially unsorted: natural order, first item is ID=1
        Assert.Equal(1, component.CurrentFirstItemId);

        // Sort by ID descending: highest ID (3) should become first
        await renderer.Dispatcher.InvokeAsync(component.SortByIdDescendingAsync);
        Assert.Equal(3, component.CurrentFirstItemId);

        // Sort by ID ascending: lowest ID (1) should become first again
        await renderer.Dispatcher.InvokeAsync(component.SortByIdAscendingAsync);
        Assert.Equal(1, component.CurrentFirstItemId);
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
            if (ShowFooter)
            {
                builder.AddAttribute(3, "FooterTemplate", (RenderFragment)(b =>
                {
                    b.AddContent(0, "Footer content");
                }));
            }
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
            if (_provider is null) { _provider = ProvideItems; }

            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "ItemsProvider", _provider);
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
            if (_provider is null) { _provider = ProvideItems; }

            builder.OpenComponent<QuickGrid<TestItem>>(0);
            builder.AddAttribute(1, "ItemsProvider", _provider);
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
