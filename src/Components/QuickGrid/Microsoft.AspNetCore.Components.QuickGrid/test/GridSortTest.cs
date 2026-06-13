// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class GridSortTest
{
    // Test model classes
    private class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime? NullableDate { get; set; }
        public int? NullableInt { get; set; }
        public TestChild Child { get; set; } = new();
    }

    private class TestChild
    {
        public string ChildName { get; set; } = string.Empty;
        public DateTime? ChildNullableDate { get; set; }
    }

    [Fact]
    public void ToPropertyName_SimpleProperty_ReturnsPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, string>> expression = x => x.Name;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("Name", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_NullableProperty_ReturnsPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, DateTime?>> expression = x => x.NullableDate;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("NullableDate", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_NullableInt_ReturnsPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, int?>> expression = x => x.NullableInt;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("NullableInt", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_NestedProperty_ReturnsNestedPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, string>> expression = x => x.Child.ChildName;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("Child.ChildName", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_NestedNullableProperty_ReturnsNestedPropertyName()
    {
        // Arrange
        Expression<Func<TestEntity, DateTime?>> expression = x => x.Child.ChildNullableDate;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("Child.ChildNullableDate", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_DescendingSort_ReturnsCorrectDirection()
    {
        // Arrange
        Expression<Func<TestEntity, DateTime?>> expression = x => x.NullableDate;

        // Act
        var gridSort = GridSort<TestEntity>.ByDescending(expression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Single(propertyList);
        Assert.Equal("NullableDate", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Descending, propertyList.First().Direction);
    }

    [Fact]
    public void ToPropertyName_MultipleSort_ReturnsAllProperties()
    {
        // Arrange
        Expression<Func<TestEntity, string>> firstExpression = x => x.Name;
        Expression<Func<TestEntity, DateTime?>> secondExpression = x => x.NullableDate;

        // Act
        var gridSort = GridSort<TestEntity>.ByAscending(firstExpression)
            .ThenDescending(secondExpression);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        // Assert
        Assert.Equal(2, propertyList.Count);

        var firstProperty = propertyList.First();
        Assert.Equal("Name", firstProperty.PropertyName);
        Assert.Equal(SortDirection.Ascending, firstProperty.Direction);

        var secondProperty = propertyList.Last();
        Assert.Equal("NullableDate", secondProperty.PropertyName);
        Assert.Equal(SortDirection.Descending, secondProperty.Direction);
    }

    [Fact]
    public void ToPropertyName_InvalidExpression_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, string>> invalidExpression = x => x.Name.ToUpper(CultureInfo.InvariantCulture);

        // Act & Assert
        var gridSort = GridSort<TestEntity>.ByAscending(invalidExpression);
        var exception = Assert.Throws<ArgumentException>(() => gridSort.ToPropertyList(ascending: true));
        Assert.Contains("The supplied expression can't be represented as a property name for sorting", exception.Message);
    }

    [Fact]
    public void ToPropertyName_MethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, string>> invalidExpression = x => x.Name.Substring(0, 1);

        // Act & Assert
        var gridSort = GridSort<TestEntity>.ByAscending(invalidExpression);
        var exception = Assert.Throws<ArgumentException>(() => gridSort.ToPropertyList(ascending: true));
        Assert.Contains("The supplied expression can't be represented as a property name for sorting", exception.Message);
    }

    [Fact]
    public void ToPropertyName_ConstantExpression_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<TestEntity, string>> invalidExpression = x => "constant";

        // Act & Assert
        var gridSort = GridSort<TestEntity>.ByAscending(invalidExpression);
        var exception = Assert.Throws<ArgumentException>(() => gridSort.ToPropertyList(ascending: true));
        Assert.Contains("The supplied expression can't be represented as a property name for sorting", exception.Message);
    }

    /// <summary>
    /// StringLengthComparer sorts strings by length (longer first), then alphabetically.
    /// Used to verify that comparer overloads work correctly.
    /// </summary>
    private class StringLengthComparer : System.Collections.Generic.IComparer<string>
    {
        public static readonly StringLengthComparer Instance = new();
        public int Compare(string x, string y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return 1;
            }
            if (y == null)
            {
                return -1;
            }

            int lengthCmp = -x.Length.CompareTo(y.Length);
            return lengthCmp != 0 ? lengthCmp : string.Compare(x, y, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ByAscending_WithComparer_SortsUsingComparer()
    {
        var data = new[] {
            new TestEntity { Name = "Alice" },
            new TestEntity { Name = "Bob" },
            new TestEntity { Name = "Charlie" }
        }.AsQueryable();

        var gridSort = GridSort<TestEntity>.ByAscending((TestEntity x) => x.Name, StringLengthComparer.Instance);

        var result = gridSort.Apply(data, ascending: true).ToArray();

        // "Charlie" (7 chars) > "Alice" (5 chars) > "Bob" (3 chars)
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Alice", result[1].Name);
        Assert.Equal("Bob", result[2].Name);
    }

    [Fact]
    public void ByDescending_WithComparer_SortsUsingComparerDescending()
    {
        var data = new[] {
            new TestEntity { Name = "Alice" },
            new TestEntity { Name = "Bob" },
            new TestEntity { Name = "Charlie" }
        }.AsQueryable();

        var gridSort = GridSort<TestEntity>.ByDescending((TestEntity x) => x.Name, StringLengthComparer.Instance);

        var result = gridSort.Apply(data, ascending: true).ToArray();

        // "Bob" (3 chars) > "Alice" (5 chars) > "Charlie" (7 chars)
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal("Alice", result[1].Name);
        Assert.Equal("Charlie", result[2].Name);
    }

    [Fact]
    public void ThenAscending_WithComparer_AppliesComparerAsSecondarySort()
    {
        var data = new[] {
            new TestEntity { Name = "Alice", Age = 30 },
            new TestEntity { Name = "Bob", Age = 25 },
            new TestEntity { Name = "Charlie", Age = 30 }
        }.AsQueryable();

        // Primary sort: by Age (default ascending). Secondary: by Name using StringLengthComparer.
        var gridSort = GridSort<TestEntity>.ByAscending((TestEntity x) => x.Age)
            .ThenAscending((TestEntity x) => x.Name, StringLengthComparer.Instance);

        var result = gridSort.Apply(data, ascending: true).ToArray();

        // Age=25 first (Bob), then Age=30: "Charlie" (7) before "Alice" (5)
        Assert.Equal("Bob", result[0].Name);   // Age=25, only one
        Assert.Equal("Charlie", result[1].Name); // Age=30, longer name first
        Assert.Equal("Alice", result[2].Name);   // Age=30, shorter name
    }

    [Fact]
    public void ThenDescending_WithComparer_AppliesComparerAsSecondarySortDescending()
    {
        var data = new[] {
            new TestEntity { Name = "Alice", Age = 30 },
            new TestEntity { Name = "Bob", Age = 25 },
            new TestEntity { Name = "Charlie", Age = 30 }
        }.AsQueryable();

        // Primary sort: by Age (ascending). Secondary: by Name descending using StringLengthComparer.
        var gridSort = GridSort<TestEntity>.ByAscending((TestEntity x) => x.Age)
            .ThenDescending((TestEntity x) => x.Name, StringLengthComparer.Instance);

        var result = gridSort.Apply(data, ascending: true).ToArray();

        // Age=25 first (Bob), then Age=30: "Alice" (5) before "Charlie" (7)
        Assert.Equal("Bob", result[0].Name);   // Age=25, only one
        Assert.Equal("Alice", result[1].Name); // Age=30, shorter name first (descending)
        Assert.Equal("Charlie", result[2].Name); // Age=30, longer name
    }

    [Fact]
    public void ByAscending_WithComparer_ToPropertyList_ReturnsCorrectPropertyName()
    {
        Expression<Func<TestEntity, string>> expression = x => x.Name;

        var gridSort = GridSort<TestEntity>.ByAscending(expression, StringLengthComparer.Instance);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        Assert.Single(propertyList);
        Assert.Equal("Name", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Ascending, propertyList.First().Direction);
    }

    [Fact]
    public void ByDescending_WithComparer_ToPropertyList_ReturnsCorrectPropertyName()
    {
        Expression<Func<TestEntity, string>> expression = x => x.Name;

        var gridSort = GridSort<TestEntity>.ByDescending(expression, StringLengthComparer.Instance);
        var propertyList = gridSort.ToPropertyList(ascending: true);

        Assert.Single(propertyList);
        Assert.Equal("Name", propertyList.First().PropertyName);
        Assert.Equal(SortDirection.Descending, propertyList.First().Direction);
    }
}
