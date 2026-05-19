// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Internal.Tests;

public class AdaptiveCapacityDictionaryTests
{
    [Fact]
    public void DefaultCtor()
    {
        // Arrange
        // Act
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Assert
        Assert.Empty(dict);
        Assert.Empty(dict._arrayStorage);
        Assert.Null(dict._dictionaryStorage);
    }

    [Fact]
    public void CreateFromNull()
    {
        // Arrange
        // Act
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Assert
        Assert.Empty(dict);
        Assert.Empty(dict._arrayStorage);
        Assert.Null(dict._dictionaryStorage);
    }

    [Fact]
    public void CreateWithCapacityOverDefaultLimit()
    {
        // The default threshold between array and dictionary is 10. If we created one over that limit it should go directly to a dictionary.
        var dict = new AdaptiveCapacityDictionary<string, string>(capacity: 12, StringComparer.OrdinalIgnoreCase);

        Assert.Null(dict._arrayStorage);
        Assert.NotNull(dict._dictionaryStorage);

        for (var i = 0; i < 12; i++)
        {
            dict[i.ToString(CultureInfo.InvariantCulture)] = i.ToString(CultureInfo.InvariantCulture);
        }

        Assert.Null(dict._arrayStorage);
        Assert.NotNull(dict._dictionaryStorage);
        Assert.Equal(12, dict.Count);
    }

    [Fact]
    public void CreateFromIEnumerableKeyValuePair_ThrowsExceptionForDuplicateKey()
    {
        // Arrange, Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new AdaptiveCapacityDictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                    {  "name", "Billy" },
                    {  "Name", "Joey" }
            },
            "key",
            $"An element with the key 'Name' already exists in the {nameof(AdaptiveCapacityDictionary<string, object?>)}.");
    }

    [Fact]
    public void Comparer_IsOrdinalIgnoreCase()
    {
        // Arrange
        // Act
        var dict = new AdaptiveCapacityDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.Same(StringComparer.OrdinalIgnoreCase, dict.Comparer);
    }

    // Our comparer is hardcoded to be IsReadOnly==false no matter what.
    [Fact]
    public void IsReadOnly_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).IsReadOnly;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IndexGet_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var value = dict[""];

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void IndexGet_EmptyStorage_ReturnsNull()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var value = dict["key"];

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void IndexGet_ArrayStorage_NoMatch_ReturnsNull()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();
        dict.Add("age", 30);

        // Act
        var value = dict["key"];

        // Assert
        Assert.Null(value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexGet_ListStorage_Match_ReturnsValue()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var value = dict["key"];

        // Assert
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexGet_ListStorage_MatchIgnoreCase_ReturnsValue()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        // Act
        var value = dict["kEy"];

        // Assert
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        dict[""] = "foo";

        // Assert
        Assert.Equal("foo", dict[""]);
    }

    [Fact]
    public void IndexSet_EmptyStorage_UpgradesToList()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_NoMatch_AddsValue()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "age", 30 },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_Match_SetsValue()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_MatchIgnoreCase_SetsValue()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Count_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var count = dict.Count;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Count_ListStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var count = dict.Count;

        // Assert
        Assert.Equal(1, count);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Keys_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        var keys = dict.Keys;

        // Assert
        Assert.Empty(keys);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Keys_ListStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var keys = dict.Keys;

        // Assert
        Assert.Equal(new[] { "key" }, keys);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Values_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        var values = dict.Values;

        // Assert
        Assert.Empty(values);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Values_ListStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var values = dict.Values;

        // Assert
        Assert.Equal(new object[] { "value" }, values);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        dict.Add("key", "value");

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        dict.Add("", "foo");

        // Assert
        Assert.Equal("foo", dict[""]);
    }

    [Fact]
    public void Add_ListStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "age", 30 },
            };

        // Act
        dict.Add("key", "value");

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_DuplicateKey()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var message = $"An element with the key 'key' already exists in the {nameof(AdaptiveCapacityDictionary<string, string>)}";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(() => dict.Add("key", "value2"), "key", message);

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_DuplicateKey_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        var message = $"An element with the key 'kEy' already exists in the {nameof(AdaptiveCapacityDictionary<string, string>)}";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(() => dict.Add("kEy", "value2"), "key", message);

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_KeyValuePair()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "age", 30 },
            };

        // Act
        ((ICollection<KeyValuePair<string, object?>>)dict).Add(new KeyValuePair<string, object?>("key", "value"));

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Clear_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
    }

    [Fact]
    public void Clear_ListStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Null(dict._dictionaryStorage);
    }

    [Fact]
    public void Contains_ListStorage_KeyValuePair_True()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Contains_ListStory_KeyValuePair_True_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("KEY", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Contains_ListStorage_KeyValuePair_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("other", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    // Value comparisons use the default equality comparer.
    [Fact]
    public void Contains_ListStorage_KeyValuePair_False_ValueComparisonIsDefault()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "valUE");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ContainsKey_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.ContainsKey("key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsKey_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.ContainsKey("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsKey_ListStorage_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("other");

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ContainsKey_ListStorage_True()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("key");

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ContainsKey_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("kEy");

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void CopyTo()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var array = new KeyValuePair<string, object?>[2];

        // Act
        ((ICollection<KeyValuePair<string, object?>>)dict).CopyTo(array, 1);

        // Assert
        Assert.Equal(
            new KeyValuePair<string, object?>[]
            {
                    default(KeyValuePair<string, object?>),
                    new KeyValuePair<string, object?>("key", "value")
            },
            array);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_True()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_True_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("KEY", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("other", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    // Value comparisons use the default equality comparer.
    [Fact]
    public void Remove_KeyValuePair_False_ValueComparisonIsDefault()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "valUE");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.Remove("key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.Remove("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_ListStorage_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("other");

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_ListStorage_True()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("key");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("kEy");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
    }

    [Fact]
    public void Remove_KeyAndOutValue_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.Remove("", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("other", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_True()
    {
        // Arrange
        object value = "value";
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", value }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        object value = "value";
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", value }
            };

        // Act
        var result = dict.Remove("kEy", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_First()
    {
        // Arrange
        object value = "value";
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", value },
                { "other", 5 },
                { "dotnet", "rocks" }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_Middle()
    {
        // Arrange
        object value = "value";
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "other", 5 },
                { "key", value },
                { "dotnet", "rocks" }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_Last()
    {
        // Arrange
        object value = "value";
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "other", 5 },
                { "dotnet", "rocks" },
                { "key", value }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryAdd_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.TryAdd("", "foo");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryAdd_EmptyStorage_CanAdd()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act
        var result = dict.TryAdd("key", "value");

        // Assert
        Assert.True(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_ArrayStorage_CanAdd()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key0", "value0" },
            };

        // Act
        var result = dict.TryAdd("key1", "value1");

        // Assert
        Assert.True(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key0", "value0"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key1", "value1"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_ArrayStorage_DoesNotAddWhenKeyIsPresent()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key0", "value0" },
            };

        // Act
        var result = dict.TryAdd("key0", "value1");

        // Assert
        Assert.False(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key0", "value0"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryGetValue_EmptyStorage()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.TryGetValue("key", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, string>();

        // Act
        var result = dict.TryGetValue("", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_ListStorage_False()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("other", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryGetValue_ListStorage_True()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("key", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryGetValue_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("kEy", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ListStorage_SwitchesToDictionaryAfter10_Add()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act 1
        dict.Add("key", "value");

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(10, storage.Length);

        // Act 2
        dict.Add("key2", "value2");
        dict.Add("key3", "value3");
        dict.Add("key4", "value4");
        dict.Add("key5", "value5");
        dict.Add("key6", "value2");
        dict.Add("key7", "value3");
        dict.Add("key8", "value4");
        dict.Add("key9", "value5");
        dict.Add("key10", "value2");
        dict.Add("key11", "value3");

        // Assert 2
        Assert.Null(dict._arrayStorage);
        Assert.Equal(11, dict.Count);
    }

    [Fact]
    public void ListStorage_SwitchesToDictionaryAfter10_TryAdd()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act 1
        dict.TryAdd("key", "value");

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(10, storage.Length);

        // Act 2
        dict.TryAdd("key2", "value2");
        dict.TryAdd("key3", "value3");
        dict.TryAdd("key4", "value4");
        dict.TryAdd("key5", "value5");
        dict.TryAdd("key6", "value2");
        dict.TryAdd("key7", "value3");
        dict.TryAdd("key8", "value4");
        dict.TryAdd("key9", "value5");
        dict.TryAdd("key10", "value2");
        dict.TryAdd("key11", "value3");

        // Assert 2
        Assert.Null(dict._arrayStorage);
        Assert.Equal(11, dict.Count);
    }

    [Fact]
    public void ListStorage_SwitchesToDictionaryAfter10_Index()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();

        // Act 1
        dict["key"] = "value";

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(10, storage.Length);

        // Act 2
        dict["key1"] = "value";
        dict["key2"] = "value";
        dict["key3"] = "value";
        dict["key4"] = "value";
        dict["key5"] = "value";
        dict["key6"] = "value";
        dict["key7"] = "value";
        dict["key8"] = "value";
        dict["key9"] = "value";
        dict["key10"] = "value";

        // Assert 2
        Assert.Null(dict._arrayStorage);
        Assert.Equal(11, dict.Count);
    }

    [Fact]
    public void ListStorage_RemoveAt_RearrangesInnerArray()
    {
        // Arrange
        var dict = new AdaptiveCapacityDictionary<string, object>();
        dict.Add("key", "value");
        dict.Add("key2", "value2");
        dict.Add("key3", "value3");

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(3, dict.Count);

        // Act
        dict.Remove("key2");

        // Assert 2
        storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(2, dict.Count);
        Assert.Equal("key", storage[0].Key);
        Assert.Equal("value", storage[0].Value);
        Assert.Equal("key3", storage[1].Key);
        Assert.Equal("value3", storage[1].Value);
    }

    [Fact]
    public void UpgradeToDictionary_KeepsComparer()
    {
        // Arrange
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new AdaptiveCapacityDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 11; i++)
        {
            dict[i.ToString(CultureInfo.InvariantCulture)] = i;
        }

        Assert.NotNull(dict._dictionaryStorage);
        Assert.Equal(comparer, dict._dictionaryStorage.Comparer);

        dict["K"] = 1;
        dict["k"] = 2;

        Assert.Equal(2, dict["K"]);
    }

    [Fact]
    public void StartAsDictionary_UsesComparer()
    {
        // Arrange
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new AdaptiveCapacityDictionary<string, object>(11, StringComparer.OrdinalIgnoreCase);

        Assert.NotNull(dict._dictionaryStorage);
        Assert.Equal(comparer, dict._dictionaryStorage.Comparer);

        dict["K"] = 1;
        dict["k"] = 2;

        Assert.Equal(2, dict["K"]);
    }

    private void AssertEmptyArrayStorage(AdaptiveCapacityDictionary<string, string> value)
    {
        Assert.Same(Array.Empty<KeyValuePair<string, object?>>(), value._arrayStorage);
    }

    private class RegularType
    {
        public bool IsAwesome { get; set; }

        public int CoolnessFactor { get; set; }
    }

    private class Visibility
    {
        private string? PrivateYo { get; set; }

        internal int ItsInternalDealWithIt { get; set; }

        public bool IsPublic { get; set; }
    }

    private class StaticProperty
    {
        public static bool IsStatic { get; set; }
    }

    private class SetterOnly
    {
        private bool _coolSetOnly;

        public bool CoolSetOnly { set { _coolSetOnly = value; } }
    }

    private class Base
    {
        public bool DerivedProperty { get; set; }
    }

    private class Derived : Base
    {
        public bool TotallySweetProperty { get; set; }
    }

    private class DerivedHiddenProperty : Base
    {
        public new int DerivedProperty { get; set; }
    }

    private class IndexerProperty
    {
        public bool this[string key]
        {
            get { return false; }
            set { }
        }
    }

    private class Address
    {
        public string? City { get; set; }

        public string? State { get; set; }
    }
}
