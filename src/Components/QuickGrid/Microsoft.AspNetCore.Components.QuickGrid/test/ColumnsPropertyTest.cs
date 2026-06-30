// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

/// <summary>
/// A derived QuickGrid class that exposes the protected Columns property for testing.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
internal class QuickGridWithAccessibleColumns<TGridItem> : QuickGrid<TGridItem>
{
    /// <summary>
    /// Gets the columns from the base class for testing verification.
    /// </summary>
    public IReadOnlyList<ColumnBase<TGridItem>> GetColumns() => Columns;

    /// <summary>
    /// Simulates the column collection process that happens during rendering.
    /// This allows tests to populate the columns list for verification.
    /// </summary>
    public void SimulateColumnCollection(List<ColumnBase<TGridItem>> columns)
    {
        var columnsField = typeof(QuickGrid<TGridItem>).GetField("_columns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var columnsList = columnsField?.GetValue(this) as System.Collections.Generic.List<ColumnBase<TGridItem>>;

        if (columnsList != null)
        {
            foreach (var column in columns)
            {
                columnsList.Add(column);
            }
        }
    }
}

/// <summary>
/// Test model for Columns property tests (issue #45398).
/// </summary>
file class ColumnsTestModel
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class ColumnsPropertyTest
{
    [Fact]
    public void Columns_ReturnsEmptyList_Initially()
    {
        var grid = new QuickGridWithAccessibleColumns<ColumnsTestModel>();
        var columns = grid.GetColumns();
        Assert.Empty(columns);
    }

    [Fact]
    public void Columns_Type_IsReadOnlyList()
    {
        var grid = new QuickGridWithAccessibleColumns<ColumnsTestModel>();
        var columns = grid.GetColumns();
        Assert.IsAssignableFrom<IReadOnlyList<ColumnBase<ColumnsTestModel>>>(columns);
    }

    [Fact]
    public void Columns_CanBeAccessedFromDerivedClass()
    {
        var grid = new QuickGridWithAccessibleColumns<ColumnsTestModel>();
        var derivedType = typeof(QuickGridWithAccessibleColumns<ColumnsTestModel>);

        var getColumnsMethod = derivedType.GetMethod(nameof(QuickGridWithAccessibleColumns<ColumnsTestModel>.GetColumns));

        Assert.NotNull(getColumnsMethod);
        var result = getColumnsMethod.Invoke(grid, null);
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<ColumnBase<ColumnsTestModel>>>(result);
    }

    [Fact]
    public void Columns_ReturnsColumns_AfterColumnsAdded()
    {
        var grid = new QuickGridWithAccessibleColumns<ColumnsTestModel>();
        var testColumns = new List<ColumnBase<ColumnsTestModel>>
        {
            CreatePropertyColumn<ColumnsTestModel, string>("Name", item => item.Name),
            CreatePropertyColumn<ColumnsTestModel, int>("Age", item => item.Age),
        };

        grid.SimulateColumnCollection(testColumns);
        var columns = grid.GetColumns();

        Assert.Equal(2, columns.Count);
        Assert.Equal("Name", columns[0].Title);
        Assert.Equal("Age", columns[1].Title);
    }

    [Fact]
    public void Columns_CanEnumerateTitles_ForExportScenario()
    {
        var grid = new QuickGridWithAccessibleColumns<ColumnsTestModel>();
        var testColumns = new List<ColumnBase<ColumnsTestModel>>
        {
            CreatePropertyColumn<ColumnsTestModel, string>("Name", item => item.Name),
            CreatePropertyColumn<ColumnsTestModel, int>("Age", item => item.Age),
        };
        grid.SimulateColumnCollection(testColumns);

        var headerRow = string.Join(",", grid.GetColumns().Select(c => c.Title));

        Assert.Equal("Name,Age", headerRow);
    }

    [Fact]
    public void PropertyColumn_Title_IsAccessible()
    {
        var propertyColumn = CreatePropertyColumn<ColumnsTestModel, string>("Name", item => item.Name);

        Assert.Equal("Name", propertyColumn.Title);
    }

    [Fact]
    public void ColumnBase_Title_IsAccessible()
    {
        var propertyColumn = CreatePropertyColumn<ColumnsTestModel, string>("Name", item => item.Name);
        var columnBase = (ColumnBase<ColumnsTestModel>)propertyColumn;

        Assert.Equal("Name", columnBase.Title);
    }

    private static PropertyColumn<TGridItem, TProp> CreatePropertyColumn<TGridItem, TProp>(string title, Expression<Func<TGridItem, TProp>> propertyExpression)
    {
        var column = new PropertyColumn<TGridItem, TProp>
        {
            Property = propertyExpression,
            Title = title
        };
        return column;
    }
}
