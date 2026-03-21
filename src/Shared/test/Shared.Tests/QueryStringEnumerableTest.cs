// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public class QueryStringEnumerableTest
{
    [Fact]
    public void ParseQueryWithUniqueKeysWorks()
    {
        Assert.Collection(Parse("?key1=value1&key2=value2"),
            kvp => AssertKeyValuePair("key1", "value1", kvp),
            kvp => AssertKeyValuePair("key2", "value2", kvp));
    }

    [Fact]
    public void ParseQueryWithoutQuestionmarkWorks()
    {
        Assert.Collection(Parse("key1=value1&key2=value2"),
            kvp => AssertKeyValuePair("key1", "value1", kvp),
            kvp => AssertKeyValuePair("key2", "value2", kvp));
    }

    [Fact]
    public void ParseQueryWithDuplicateKeysGroups()
    {
        Assert.Collection(Parse("?key1=valueA&key2=valueB&key1=valueC"),
            kvp => AssertKeyValuePair("key1", "valueA", kvp),
            kvp => AssertKeyValuePair("key2", "valueB", kvp),
            kvp => AssertKeyValuePair("key1", "valueC", kvp));
    }

    [Fact]
    public void ParseQueryWithEmptyValuesWorks()
    {
        Assert.Collection(Parse("?key1=&key2="),
            kvp => AssertKeyValuePair("key1", string.Empty, kvp),
            kvp => AssertKeyValuePair("key2", string.Empty, kvp));
    }

    [Fact]
    public void ParseQueryWithEmptyKeyWorks()
    {
        Assert.Collection(Parse("?=value1&="),
            kvp => AssertKeyValuePair(string.Empty, "value1", kvp),
            kvp => AssertKeyValuePair(string.Empty, string.Empty, kvp));
    }

    [Fact]
    public void ParseQueryWithEncodedKeyWorks()
    {
        Assert.Collection(Parse("?fields+%5BtodoItems%5D"),
            kvp => AssertKeyValuePair("fields+%5BtodoItems%5D", string.Empty, kvp));
    }

    [Fact]
    public void ParseQueryWithEncodedValueWorks()
    {
        Assert.Collection(Parse("?=fields+%5BtodoItems%5D"),
            kvp => AssertKeyValuePair(string.Empty, "fields+%5BtodoItems%5D", kvp));
    }

    [Theory]
    [InlineData("?")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("?&&")]
    public void ParseEmptyOrNullQueryWorks(string? queryString)
    {
        Assert.Empty(Parse(queryString));
    }

    [Fact]
    public void ParseIgnoresEmptySegments()
    {
        Assert.Collection(Parse("?&key1=value1&&key2=value2&"),
            kvp => AssertKeyValuePair("key1", "value1", kvp),
            kvp => AssertKeyValuePair("key2", "value2", kvp));
    }

    [Theory]
    [InlineData("?a+b=c+d", "a b", "c d")]
    [InlineData("? %5Bkey%5D = %26value%3D ", " [key] ", " &value= ")]
    [InlineData("?+", " ", "")]
    [InlineData("?=+", "", " ")]
    public void DecodingWorks(string queryString, string expectedDecodedName, string expectedDecodedValue)
    {
        foreach (var kvp in new QueryStringEnumerable(queryString))
        {
            Assert.Equal(expectedDecodedName, kvp.DecodeName().ToString());
            Assert.Equal(expectedDecodedValue, kvp.DecodeValue().ToString());
        }
    }

    [Fact]
    public void DecodingReusesMemoryIfDecodingNotNeeded()
    {
        foreach (var kvp in new QueryStringEnumerable("?key=value"))
        {
            Assert.True(kvp.EncodedName.Equals(kvp.DecodeName()));
            Assert.True(kvp.EncodedValue.Equals(kvp.DecodeValue()));
        }
    }

    private static void AssertKeyValuePair(string expectedKey, string expectedValue, (string key, string value) actual)
    {
        Assert.Equal(expectedKey, actual.key);
        Assert.Equal(expectedValue, actual.value);
    }

    private static IReadOnlyList<(string key, string value)> Parse(string query)
    {
        var result = new List<(string key, string value)>();
        var enumerable = new QueryStringEnumerable(query);
        foreach (var pair in enumerable)
        {
            result.Add((pair.EncodedName.ToString(), pair.EncodedValue.ToString()));
        }

        return result;
    }
}
