// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class CopyOnWriteDictionaryHolderTest
{
    [Fact]
    public void ReadOperation_DelegatesToSourceDictionary_IfNoMutationsArePerformed()
    {
        // Arrange
        var source = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" },
                { "key2", "key2-value" }
            };

        var holder = new CopyOnWriteDictionaryHolder<string, object>(source);

        // Act and Assert
        Assert.Equal("key2-value", holder["key2"]);
        Assert.Equal(2, holder.Count);
        Assert.Equal(new string[] { "test-key", "key2" }, holder.Keys.ToArray());
        Assert.Equal(new object[] { "test-value", "key2-value" }, holder.Values.ToArray());
        Assert.True(holder.ContainsKey("test-key"));

        object value;
        Assert.False(holder.TryGetValue("different-key", out value));

        Assert.False(holder.HasBeenCopied);
        Assert.Same(source, holder.ReadDictionary);
    }

    [Fact]
    public void ReadOperation_DoesNotDelegateToSourceDictionary_OnceAValueIsChanged()
    {
        // Arrange
        var source = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

        var holder = new CopyOnWriteDictionaryHolder<string, object>(source);

        // Act
        holder["key2"] = "value3";

        // Assert
        Assert.Equal("value2", source["key2"]);
        Assert.Equal(2, holder.Count);
        Assert.Equal("value1", holder["key1"]);
        Assert.Equal("value3", holder["key2"]);

        Assert.True(holder.HasBeenCopied);
        Assert.NotSame(source, holder.ReadDictionary);
    }

    [Fact]
    public void ReadOperation_DoesNotDelegateToSourceDictionary_OnceValueIsAdded()
    {
        // Arrange
        var source = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

        var holder = new CopyOnWriteDictionaryHolder<string, object>(source);

        // Act
        holder.Add("key3", "value3");
        holder.Remove("key1");

        // Assert
        Assert.Equal(2, source.Count);
        Assert.Equal("value1", source["key1"]);
        Assert.Equal(2, holder.Count);
        Assert.Equal("value2", holder["KeY2"]);
        Assert.Equal("value3", holder["key3"]);

        Assert.True(holder.HasBeenCopied);
        Assert.NotSame(source, holder.ReadDictionary);
    }
}
