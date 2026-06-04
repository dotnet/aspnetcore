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
}
