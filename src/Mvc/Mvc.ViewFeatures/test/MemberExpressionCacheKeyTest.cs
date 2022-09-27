// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class MemberExpressionCacheKeyTest
{
    [Fact]
    public void GetEnumerator_ReturnsMembers()
    {
        // Arrange
        var expected = new[]
        {
                typeof(TestModel3).GetProperty(nameof(TestModel3.Value)),
                typeof(TestModel2).GetProperty(nameof(TestModel2.TestModel3)),
                typeof(TestModel).GetProperty(nameof(TestModel.TestModel2)),
            };

        var key = GetKey(m => m.TestModel2.TestModel3.Value);

        // Act
        var actual = GetMembers(key);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetEnumerator_WithNullableType_ReturnsMembers()
    {
        // Arrange
        var expected = new[]
        {
                typeof(DateTime).GetProperty(nameof(DateTime.Ticks)),
                typeof(DateTime?).GetProperty(nameof(Nullable<DateTime>.Value)),
                typeof(TestModel).GetProperty(nameof(TestModel.NullableDateTime)),
            };

        var key = GetKey(m => m.NullableDateTime.Value.Ticks);

        // Act
        var actual = GetMembers(key);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetEnumerator_WithValueType_ReturnsMembers()
    {
        // Arrange
        var expected = new[]
        {
                typeof(DateTime).GetProperty(nameof(DateTime.Ticks)),
                typeof(TestModel).GetProperty(nameof(TestModel.DateTime)),
            };

        var key = GetKey(m => m.DateTime.Ticks);

        // Act
        var actual = GetMembers(key);

        // Assert
        Assert.Equal(expected, actual);
    }

    private static MemberExpressionCacheKey GetKey<TResult>(Expression<Func<TestModel, TResult>> expression)
    {
        var memberExpression = Assert.IsAssignableFrom<MemberExpression>(expression.Body);
        return new MemberExpressionCacheKey(typeof(TestModel), memberExpression);
    }

    private static IList<MemberInfo> GetMembers(MemberExpressionCacheKey key)
    {
        var members = new List<MemberInfo>();
        foreach (var member in key)
        {
            members.Add(member);
        }

        return members;
    }

    public class TestModel
    {
        public TestModel2 TestModel2 { get; set; }

        public DateTime DateTime { get; set; }

        public DateTime? NullableDateTime { get; set; }
    }

    public class TestModel2
    {
        public string Name { get; set; }

        public TestModel3 TestModel3 { get; set; }
    }

    public class TestModel3
    {
        public string Value { get; set; }
    }
}
