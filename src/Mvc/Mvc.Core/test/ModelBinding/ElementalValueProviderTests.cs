// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ElementalValueProviderTest
{
    [Theory]
    [InlineData("MyProperty", "MyProperty")]
    [InlineData("MyProperty.SubProperty", "MyProperty")]
    [InlineData("MyProperty[0]", "MyProperty")]
    public void ContainsPrefix_ReturnsTrue_IfElementNameStartsWithPrefix(
        string elementName,
        string prefix)
    {
        // Arrange
        var culture = new CultureInfo("en-US");
        var elementalValueProvider = new ElementalValueProvider(
            elementName,
            "hi",
            culture);

        // Act
        var containsPrefix = elementalValueProvider.ContainsPrefix(prefix);

        // Assert
        Assert.True(containsPrefix);
    }

    [Theory]
    [InlineData("MyProperty", "MyProperty1")]
    [InlineData("MyPropertyTest", "MyProperty")]
    [InlineData("Random", "MyProperty")]
    public void ContainsPrefix_ReturnsFalse_IfElementCannotSpecifyValuesForPrefix(
        string elementName,
        string prefix)
    {
        // Arrange
        var culture = new CultureInfo("en-US");
        var elementalValueProvider = new ElementalValueProvider(
            elementName,
            "hi",
            culture);

        // Act
        var containsPrefix = elementalValueProvider.ContainsPrefix(prefix);

        // Assert
        Assert.False(containsPrefix);
    }

    [Fact]
    public void GetValue_NameDoesNotMatch_ReturnsEmptyResult()
    {
        // Arrange
        var culture = new CultureInfo("fr-FR");
        var valueProvider = new ElementalValueProvider("foo", "hi", culture);

        // Act
        var result = valueProvider.GetValue("bar");

        // Assert
        Assert.Equal(ValueProviderResult.None, result);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("FOO")]
    [InlineData("FoO")]
    public void GetValue_NameMatches_ReturnsValueProviderResult(string name)
    {
        // Arrange
        var culture = new CultureInfo("fr-FR");
        var valueProvider = new ElementalValueProvider("foo", "hi", culture);

        // Act
        var result = valueProvider.GetValue(name);

        // Assert
        Assert.Equal("hi", (string)result);
        Assert.Equal(culture, result.Culture);
    }
}
