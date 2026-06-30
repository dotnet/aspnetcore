// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class ColumnBaseTest
{
    // Test model classes
    private class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private class TestColumn : ColumnBase<TestEntity>
    {
        public TestColumn()
        {
        }

        protected internal override void CellContent(RenderTreeBuilder builder, TestEntity item)
        {
            throw new NotImplementedException();
        }
        public override GridSort<TestEntity> SortBy { get; set; }
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_DefaultValues_ReturnsBaseClassOnly()
    {
        var column = new TestColumn();
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Equal("col-options-button", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithColumnOptionsButtonClass_ReturnsCombinedClasses()
    {
        var column = new TestColumn { ColumnOptionsButtonClass = "custom-class" };
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("custom-class", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithColumnOptionsActive_ReturnsActiveClass()
    {
        var column = new TestColumn { ColumnOptionsActive = true };
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("col-options-active", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithAllProperties_ReturnsAllClasses()
    {
        var column = new TestColumn
        {
            ColumnOptionsButtonClass = "custom-class",
            ColumnOptionsActive = true
        };
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("custom-class", cssClass);
        Assert.Contains("col-options-active", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithEmptyColumnOptionsButtonClass_ReturnsBaseAndActiveOnly()
    {
        var column = new TestColumn
        {
            ColumnOptionsButtonClass = string.Empty,
            ColumnOptionsActive = true
        };
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("col-options-active", cssClass);
        Assert.DoesNotContain("  ", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithNullColumnOptionsButtonClass_ReturnsBaseAndActiveOnly()
    {
        var column = new TestColumn
        {
            ColumnOptionsButtonClass = null,
            ColumnOptionsActive = true
        };
        var cssClass = column.ColumnOptionsButtonCssClass;

        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("col-options-active", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_ActiveFalse_DoesNotIncludeActiveClass()
    {
        var column = new TestColumn
        {
            ColumnOptionsButtonClass = "custom-class",
            ColumnOptionsActive = false
        };

        var cssClass = column.ColumnOptionsButtonCssClass;
        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("custom-class", cssClass);
        Assert.DoesNotContain("col-options-active", cssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_WithWhitespaceInCustomClass_TrimsProperly()
    {
        var column = new TestColumn
        {
            ColumnOptionsButtonClass = "  custom-class  "
        };
        var cssClass = column.ColumnOptionsButtonCssClass;
        Assert.DoesNotContain("   ", cssClass);
    }

    [Fact]
    public void ColumnHeaderTemplate_RendersButtonWithCorrectClasses()
    {
        var column = new TestColumn
        {
            ColumnOptionsActive = true,
            ColumnOptionsButtonClass = "my-filter-indicator"
        };

        var builder = new RenderTreeBuilder();
        column.HeaderContent(builder);

        var cssClass = column.ColumnOptionsButtonCssClass;
        Assert.Contains("col-options-button", cssClass);
        Assert.Contains("my-filter-indicator", cssClass);
        Assert.Contains("col-options-active", cssClass);
    }
}
