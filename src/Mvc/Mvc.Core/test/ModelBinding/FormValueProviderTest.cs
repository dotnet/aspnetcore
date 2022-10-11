// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class FormValueProviderTest : EnumerableValueProviderTest
{
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

    protected override IEnumerableValueProvider GetEnumerableValueProvider(
        BindingSource bindingSource,
        Dictionary<string, StringValues> values,
        CultureInfo culture)
    {
        var backingStore = new FormCollection(values);
        return new FormValueProvider(bindingSource, backingStore, culture);
    }
}
