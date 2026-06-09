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
}
