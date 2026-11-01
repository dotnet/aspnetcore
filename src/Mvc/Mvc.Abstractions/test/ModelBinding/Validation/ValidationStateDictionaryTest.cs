// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ValidationStateDictionaryTest
{
    [Fact]
    public void EmptyDictionary_ReadOperations_WithoutBackingDictionary()
    {
        // Arrange
        // Only members that do not create the backing dictionary are used here.
        var dictionary = new ValidationStateDictionary();
        var model = new object();

        // Act & Assert
        Assert.True(dictionary.Count == 0);
        Assert.False(dictionary.ContainsKey(model));
        Assert.False(dictionary.TryGetValue(model, out var entry));
        Assert.Null(entry);
        Assert.Null(dictionary[model]);
        dictionary.Clear();
        Assert.True(dictionary.Count == 0);
        Assert.False(dictionary.ContainsKey(model));
    }

    [Fact]
    public void EmptyDictionary_ReadOperations()
    {
        // Arrange
        var dictionary = new ValidationStateDictionary();
        var model = new object();

        // Act & Assert
        Assert.Empty(dictionary);
        Assert.False(dictionary.IsReadOnly);
        Assert.False(dictionary.Remove(model));
        Assert.Empty(dictionary.Keys);
        Assert.Empty(dictionary.Values);
        Assert.False(dictionary.ContainsKey(model));
        Assert.Null(dictionary[model]);
        dictionary.Clear();
        Assert.True(dictionary.Count == 0);
    }

    [Fact]
    public void EmptyDictionary_NullKey_Throws()
    {
        // Arrange
        var dictionary = new ValidationStateDictionary();

        // Act & Assert
        // Before any entry is added, the backing dictionary does not exist yet.
        Assert.Throws<ArgumentNullException>("key", () => dictionary.ContainsKey(null));
        Assert.Throws<ArgumentNullException>("key", () => dictionary.TryGetValue(null, out _));
        Assert.Throws<ArgumentNullException>("key", () => dictionary[null]);

        dictionary.Add(new object(), new ValidationStateEntry());
        Assert.Throws<ArgumentNullException>("key", () => dictionary.ContainsKey(null));
        Assert.Throws<ArgumentNullException>("key", () => dictionary.TryGetValue(null, out _));
        Assert.Throws<ArgumentNullException>("key", () => dictionary[null]);
    }

    [Fact]
    public void KeysAndValues_ReflectEntriesAddedLater()
    {
        // Arrange
        var dictionary = new ValidationStateDictionary();
        var keys = dictionary.Keys;
        var values = dictionary.Values;
        var model = new object();
        var entry = new ValidationStateEntry();

        // Act
        dictionary[model] = entry;

        // Assert
        Assert.Same(model, Assert.Single(keys));
        Assert.Same(entry, Assert.Single(values));
        Assert.Same(entry, dictionary[model]);
        Assert.True(dictionary.ContainsKey(model));
        Assert.True(dictionary.Count == 1);
    }

    [Fact]
    public void UsesReferenceEquality()
    {
        // Arrange
        var dictionary = new ValidationStateDictionary();
        var model = "value";
        var equalModel = new string("value".AsSpan());
        var entry = new ValidationStateEntry();

        // Act
        dictionary.Add(model, entry);

        // Assert
        Assert.True(dictionary.ContainsKey(model));
        Assert.False(dictionary.ContainsKey(equalModel));
    }
}
