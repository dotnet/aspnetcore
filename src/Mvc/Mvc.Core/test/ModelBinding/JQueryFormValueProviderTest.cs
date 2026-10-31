// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class JQueryFormValueProviderTest : EnumerableValueProviderTest
{
    protected override IEnumerableValueProvider GetEnumerableValueProvider(
        BindingSource bindingSource,
        Dictionary<string, StringValues> values,
        CultureInfo culture)
    {
        return new JQueryFormValueProvider(bindingSource, values, culture);
    }

    [Fact]
    public void Filter_ExcludesItself()
    {
        // Arrange
        var dictionary = new Dictionary<string, StringValues>();
        var provider = new JQueryFormValueProvider(BindingSource.Form, dictionary, CultureInfo.CurrentCulture);

        // Act
        var result = provider.Filter();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public override void GetValue_EmptyKey()
    {
        // Arrange
        var store = new Dictionary<string, StringValues>(BackingStore)
            {
                { string.Empty, "some-value" },
            };
        var valueProvider = GetEnumerableValueProvider(BindingSource.Query, store, culture: null);

        // Act
        var result = valueProvider.GetValue(string.Empty);

        // Assert
        Assert.Equal("some-value", (string)result);
    }

    [Fact]
    public void GetValue_ReturnsInvariantCulture_IfInvariantEntryExists()
    {
        // Arrange
        var culture = new CultureInfo("fr-FR");
        var invariantCultureKey = "prefix.name";
        var currentCultureKey = "some";
        var values = new Dictionary<string, StringValues>(BackingStore)
        {
            { FormValueHelper.CultureInvariantFieldName, new(invariantCultureKey) },
        };
        var valueProvider = GetEnumerableValueProvider(BindingSource.Query, values, culture);

        // Act
        var invariantCultureResult = valueProvider.GetValue(invariantCultureKey);
        var currentCultureResult = valueProvider.GetValue(currentCultureKey);

        // Assert
        Assert.Equal(CultureInfo.InvariantCulture, invariantCultureResult.Culture);
        Assert.Equal(BackingStore[invariantCultureKey], invariantCultureResult.Values);

        Assert.Equal(culture, currentCultureResult.Culture);
        Assert.Equal(BackingStore[currentCultureKey], currentCultureResult.Values);
    }
}
