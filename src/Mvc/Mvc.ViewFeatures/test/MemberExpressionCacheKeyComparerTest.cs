// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class MemberExpressionCacheKeyComparerTest
{
    private readonly MemberExpressionCacheKeyComparer Comparer = MemberExpressionCacheKeyComparer.Instance;

    [Fact]
    public void Equals_ReturnsTrue_ForTheSameExpression()
    {
        // Arrange
        var key = GetKey(m => m.Value);

        // Act & Assert
        VerifyEquals(key, key);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForDifferentInstances_OfSameExpression()
    {
        // Arrange
        var key1 = GetKey(m => m.Value);
        var key2 = GetKey(m => m.Value);

        // Act & Assert
        VerifyEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForChainedMemberAccessExpressionsWithReferenceTypes()
    {
        // Arrange
        var key1 = GetKey(m => m.TestModel2.Name);
        var key2 = GetKey(m => m.TestModel2.Name);

        // Act & Assert
        VerifyEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForChainedMemberAccessExpressionsWithNullableValueTypes()
    {
        // Arrange
        var key1 = GetKey(m => m.NullableDateTime.Value.TimeOfDay);
        var key2 = GetKey(m => m.NullableDateTime.Value.TimeOfDay);

        // Act & Assert
        VerifyEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForChainedMemberAccessExpressionsWithValueTypes()
    {
        // Arrange
        var key1 = GetKey(m => m.DateTime.Year);
        var key2 = GetKey(m => m.DateTime.Year);

        // Act & Assert
        VerifyEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentExpression()
    {
        // Arrange
        var key1 = GetKey(m => m.Value);
        var key2 = GetKey(m => m.TestModel2.Name);

        // Act & Assert
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForChainedExpressions()
    {
        // Arrange
        var key1 = GetKey(m => m.TestModel2.Id);
        var key2 = GetKey(m => m.TestModel2.Name);

        // Act & Assert
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForChainedExpressions_WithValueTypes()
    {
        // Arrange
        var key1 = GetKey(m => m.DateTime.Ticks);
        var key2 = GetKey(m => m.DateTime.Year);

        // Act & Assert
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForChainedExpressions_DifferingByNullable()
    {
        // Arrange
        var key1 = GetKey(m => m.DateTime.Ticks);
        var key2 = GetKey(m => m.NullableDateTime.Value.Ticks);

        // Act & Assert
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOneExpressionIsSubsetOfOther()
    {
        // Arrange
        var key1 = GetKey(m => m.TestModel2);
        var key2 = GetKey(m => m.TestModel2.Name);

        // Act & Assert
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMemberIsAccessedThroughNullableProperty()
    {
        // Arrange
        var key1 = GetKey(m => m.NullableDateTime.Value.Year);
        var key2 = GetKey(m => m.DateTime.Year);

        // Act
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMemberIsAccessedThroughDifferentModels()
    {
        // Arrange
        var key1 = GetKey<TestModel2, int>(m => m.Id);
        var key2 = GetKey(m => m.TestModel2.Id);

        // Act
        VerifyNotEquals(key1, key2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMemberIsAccessedThroughConstantExpression()
    {
        // Arrange
        var testModel = new TestModel2 { Id = 1 };
        var key1 = GetKey(m => testModel.Id);
        var key2 = GetKey<TestModel2, int>(m => m.Id);

        // Act
        VerifyNotEquals(key1, key2);
    }

    private void VerifyEquals(MemberExpressionCacheKey key1, MemberExpressionCacheKey key2)
    {
        Assert.Equal(key1, key2, Comparer);

        var hashCode1 = Comparer.GetHashCode(key1);
        var hashCode2 = Comparer.GetHashCode(key2);
        Assert.Equal(hashCode1, hashCode2);

        var cachedKey1 = key1.MakeCacheable();

        Assert.Equal(key1, cachedKey1, Comparer);
        Assert.Equal(cachedKey1, key1, Comparer);

        var cachedKeyHashCode1 = Comparer.GetHashCode(cachedKey1);
        Assert.Equal(hashCode1, cachedKeyHashCode1);
    }

    private void VerifyNotEquals(MemberExpressionCacheKey key1, MemberExpressionCacheKey key2)
    {
        var hashCode1 = Comparer.GetHashCode(key1);
        var hashCode2 = Comparer.GetHashCode(key2);

        Assert.NotEqual(hashCode1, hashCode2);
        Assert.NotEqual(key1, key2, Comparer);

        var cachedKey1 = key1.MakeCacheable();
        Assert.NotEqual(key2, cachedKey1, Comparer);

        var cachedKeyHashCode1 = Comparer.GetHashCode(cachedKey1);
        Assert.NotEqual(cachedKeyHashCode1, hashCode2);
    }

    private static MemberExpressionCacheKey GetKey<TResult>(Expression<Func<TestModel, TResult>> expression)
        => GetKey<TestModel, TResult>(expression);

    private static MemberExpressionCacheKey GetKey<TModel, TResult>(Expression<Func<TModel, TResult>> expression)
    {
        var memberExpression = Assert.IsAssignableFrom<MemberExpression>(expression.Body);
        return new MemberExpressionCacheKey(typeof(TModel), memberExpression);
    }

    public class TestModel
    {
        public string Value { get; set; }

        public TestModel2 TestModel2 { get; set; }

        public DateTime DateTime { get; set; }

        public DateTime? NullableDateTime { get; set; }
    }

    public class TestModel2
    {
        public string Name { get; set; }

        public int Id { get; set; }
    }
}
