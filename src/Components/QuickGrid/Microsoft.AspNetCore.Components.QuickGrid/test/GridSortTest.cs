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
}