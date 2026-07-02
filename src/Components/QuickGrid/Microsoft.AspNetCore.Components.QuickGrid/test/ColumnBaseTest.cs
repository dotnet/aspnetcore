// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

/// <summary>
/// Tests for ColumnBase and derived column types.
/// </summary>
public class ColumnBaseTest
{
    [Fact]
    public void ColumnBase_IsSortableByDefault_ReturnsFalse()
    {
        var column = new TestColumn();

        var result = column.IsSortableByDefault();

        Assert.False(result);
    }

    [Fact]
    public void TemplateColumn_IsSortableByDefault_ReturnsFalseWhenNoSortBy()
    {
        var column = new TemplateColumn<TestItem>
        {
            ChildContent = _ => null
        };

        var result = column.IsSortableByDefault();

        Assert.False(result);
    }

    [Fact]
    public void TemplateColumn_IsSortableByDefault_ReturnsTrueWhenSortBySpecified()
    {
        var column = new TemplateColumn<TestItem>
        {
            ChildContent = _ => null,
            SortBy = GridSort<TestItem>.ByAscending(x => x.Name)
        };

        var result = column.IsSortableByDefault();

        Assert.True(result);
    }

    [Fact]
    public void ColumnBase_IsSortableByDefault_OverrideInDerivedClass_Works()
    {
        var customColumn = new CustomSortableColumn<TestItem>();

        var result = customColumn.IsSortableByDefault();

        Assert.True(result);
    }

    private class TestItem
    {
        public string Name { get; set; }
        = string.Empty;
    }

    private class TestColumn : ColumnBase<TestItem>
    {
        public override GridSort<TestItem> SortBy { get; set; }
        protected internal override void CellContent(RenderTreeBuilder builder, TestItem item)
        {
            throw new NotImplementedException();
        }
    }

    private class CustomSortableColumn<T> : ColumnBase<T>
    {
        public override GridSort<T> SortBy { get; set; }

        public override bool IsSortableByDefault()
            => true;

        protected internal override void CellContent(RenderTreeBuilder builder, T item)
        {
            throw new NotImplementedException();
        }
    }
}
