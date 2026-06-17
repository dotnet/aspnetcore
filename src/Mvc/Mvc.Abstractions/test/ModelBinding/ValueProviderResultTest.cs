// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ValueProviderResultTest
{
    [Fact]
    public void Construct_With_NullString()
    {
        // Arrange & Act
        var result = new ValueProviderResult((string)null);

        // Assert
        Assert.Equal(0, result.Length);
        Assert.Equal(0, result.Values.Count);
        Assert.Null(result.FirstValue);
        Assert.Equal(ValueProviderResult.None, result);
        Assert.Empty((string)result);
        Assert.Empty((string[])result);
    }

    [Fact]
    public void Construct_With_NullArray()
    {
        // Arrange & Act
        var result = new ValueProviderResult((string[])null);

        // Assert
        Assert.Equal(0, result.Length);
        Assert.Equal(0, result.Values.Count);
        Assert.Null(result.FirstValue);
        Assert.Equal(ValueProviderResult.None, result);
        Assert.Empty((string)result);
        Assert.Empty((string[])result);
    }

    [Fact]
    public void Construct_With_None()
    {
        // Arrange & Act
        var result = ValueProviderResult.None;

        // Assert
        Assert.Equal(0, result.Length);
        Assert.Equal(0, result.Values.Count);
        Assert.Null(result.FirstValue);
        Assert.Equal(ValueProviderResult.None, result);
        Assert.Equal(ValueProviderResult.None, new ValueProviderResult(new StringValues()));
        Assert.Empty((string)result);
        Assert.Empty((string[])result);
    }

    [Fact]
    public void Construct_With_String()
    {
        // Arrange & Act
        var result = new ValueProviderResult("Hi There");

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal("Hi There", result.Values);
        Assert.Equal("Hi There", result.FirstValue);
        Assert.NotEqual(ValueProviderResult.None, result);
        Assert.Equal("Hi There", (string)result);
        Assert.Equal(new string[] { "Hi There" }, (string[])result);
    }

    [Fact]
    public void Construct_With_Array()
    {
        // Arrange & Act
        var result = new ValueProviderResult(new string[] { "Hi", "There" });

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(new string[] { "Hi", "There" }, result.Values.ToArray());
        Assert.Equal("Hi", result.FirstValue);
        Assert.NotEqual(ValueProviderResult.None, result);
        Assert.Equal("Hi,There", (string)result);
        Assert.Equal(new string[] { "Hi", "There" }, (string[])result);
    }

    [Fact]
    public void Enumerator_WithString()
    {
        // Arrange
        var result = new ValueProviderResult("Hi There");

        // Act & Assert
        Assert.Equal<string>(new string[] { "Hi There", }, result);
    }

    [Fact]
    public void Enumerator_WithArray()
    {
        // Arrange
        var result = new ValueProviderResult(new string[] { "Hi", "There" });

        // Act & Assert
        Assert.Equal<string>(new string[] { "Hi", "There" }, result);
    }

    public static TheoryData<ValueProviderResult, ValueProviderResult, bool> EqualsData
    {
        get
        {
            return new TheoryData<ValueProviderResult, ValueProviderResult, bool>()
                {
                    {
                        new ValueProviderResult("Hi"),
                        new ValueProviderResult("Hi"),
                        true
                    },
                    {
                        new ValueProviderResult("Hi"),
                        new ValueProviderResult(new string[] { "Hi"}),
                        true
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi"}),
                        new ValueProviderResult("Hi"),
                        true
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi"}),
                        new ValueProviderResult(new string[] { "Hi"}),
                        true
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi", "There"}),
                        new ValueProviderResult(new string[] { "Hi", "There"}),
                        true
                    },
                    {
                        new ValueProviderResult("Hi,There"),
                        new ValueProviderResult(new string[] { "Hi", "There"}),
                        false
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi", string.Empty }),
                        new ValueProviderResult(new string[] { "Hi", "There"}),
                        false
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi", "There" }),
                        new ValueProviderResult(new string[] { "Hi", "ThEre"}),
                        false
                    },
                    {
                        new ValueProviderResult(new string[] { "Hi", }),
                        new ValueProviderResult(new string[] { "Hi", string.Empty }),
                        false
                    },
                    {
                        new ValueProviderResult(),
                        new ValueProviderResult((string)null),
                        true
                    },
                    {
                        new ValueProviderResult(),
                        new ValueProviderResult("hi"),
                        false
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EqualsData))]
    public void Operator_Equals(ValueProviderResult x, ValueProviderResult y, bool expected)
    {
        // Arrange
        var result = x == y;

        // Act & Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(EqualsData))]
    public void Operator_NotEquals(ValueProviderResult x, ValueProviderResult y, bool expected)
    {
        // Arrange
        var result = x != y;

        // Act & Assert
        Assert.NotEqual(expected, result);
    }
}
