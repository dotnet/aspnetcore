// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class QuickGridTest
{
    [Theory]
    [InlineData(Align.Start, "col-justify-start", true)]
    [InlineData(Align.Center, "col-justify-center", true)]
    [InlineData(Align.End, "col-justify-end", true)]
    [InlineData(Align.Left, "col-justify-left", true)]
    [InlineData(Align.Right, "col-justify-right", true)]
    [InlineData(Align.Start, "col-justify-start", false)]
    [InlineData(Align.Center, "col-justify-center", false)]
    [InlineData(Align.End, "col-justify-end", false)]
    [InlineData(Align.Left, "col-justify-left", false)]
    [InlineData(Align.Right, "col-justify-right", false)]
    public Task UnsetColumnClassesDoNotAddTrailingWhitespace(Align align, string expectedClass, bool nullableItems)
        => nullableItems
            ? AssertColumnClassesAsync(new int?[] { null, 0 }, align, expectedClass)
            : AssertColumnClassesAsync(new[] { 0, 1 }, align, expectedClass);

    private static async Task AssertColumnClassesAsync<TGridItem>(TGridItem[] items, Align align, string expectedClass)
    {
        using var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(Mock.Of<IJSRuntime>())
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<PropertyColumn<TGridItem, TGridItem>>(0);
            builder.AddAttribute(1, nameof(PropertyColumn<TGridItem, TGridItem>.Property),
                (Expression<Func<TGridItem, TGridItem>>)(item => item));
            builder.AddAttribute(2, nameof(ColumnBase<TGridItem>.Align), align);
            builder.CloseComponent();
            builder.OpenComponent<TemplateColumn<TGridItem>>(3);
            builder.AddAttribute(4, nameof(ColumnBase<TGridItem>.Align), align);
            builder.AddAttribute(5, nameof(TemplateColumn<TGridItem>.ChildContent),
                (RenderFragment<TGridItem>)(item => content => content.AddContent(0, item)));
            builder.CloseComponent();
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(QuickGrid<TGridItem>.Items)] = items.AsQueryable(),
            [nameof(QuickGrid<TGridItem>.Pagination)] = new PaginationState { ItemsPerPage = 3 },
            [nameof(QuickGrid<TGridItem>.ChildContent)] = columns,
        });

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<QuickGrid<TGridItem>>(parameters);
            var html = result.ToHtmlString();

            Assert.Equal(6, html.Split($"<td class=\"{expectedClass}\"").Length - 1);
            Assert.Equal(2, html.Split($"<th class=\"{expectedClass}\"").Length - 1);
            Assert.DoesNotContain($"class=\"{expectedClass} ", html);
        });
    }

    [Fact]
    public async Task CellClassAppliesToNullDataRowsButNotHeadersOrFillerRows()
    {
        using var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(Mock.Of<IJSRuntime>())
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var callbackItems = new List<int?>();
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<TemplateColumn<int?>>(0);
            builder.AddAttribute(1, nameof(TemplateColumn<int?>.Class), "static");
            builder.AddAttribute(2, nameof(TemplateColumn<int?>.Align), Align.Center);
            builder.AddAttribute(3, nameof(TemplateColumn<int?>.CellClass), (Func<int?, string>)(item =>
            {
                callbackItems.Add(item);
                return item is null ? "missing" : "present";
            }));
            builder.AddAttribute(4, nameof(TemplateColumn<int?>.ChildContent),
                (RenderFragment<int?>)(item => content => content.AddContent(0, item?.ToString(CultureInfo.InvariantCulture) ?? "missing")));
            builder.CloseComponent();
            builder.OpenComponent<TemplateColumn<int?>>(5);
            builder.AddAttribute(6, nameof(TemplateColumn<int?>.Class), "without-callback");
            builder.AddAttribute(7, nameof(TemplateColumn<int?>.ChildContent),
                (RenderFragment<int?>)(item => content => content.AddContent(0, item?.ToString(CultureInfo.InvariantCulture) ?? "missing")));
            builder.CloseComponent();
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(QuickGrid<int?>.Items)] = new int?[] { null, 0, 1 }.AsQueryable(),
            [nameof(QuickGrid<int?>.Pagination)] = new PaginationState { ItemsPerPage = 4 },
            [nameof(QuickGrid<int?>.ChildContent)] = columns,
        });

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<QuickGrid<int?>>(parameters);
            var html = result.ToHtmlString();

            Assert.Contains("<td class=\"col-justify-center static missing\"", html);
            Assert.Equal(2, html.Split("<td class=\"col-justify-center static present\"").Length - 1);
            Assert.Contains("<th class=\"col-justify-center static\"", html);
            Assert.Equal(1, html.Split("<td class=\"col-justify-center static\"").Length - 1);
            Assert.Equal(4, html.Split("<td class=\"col-justify-start without-callback\"").Length - 1);
            Assert.Contains((int?)null, callbackItems);
            Assert.Contains((int?)0, callbackItems);
            Assert.Contains((int?)1, callbackItems);
        });
    }
}
