// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class JQueryQueryStringValueProviderTest : EnumerableValueProviderTest
{
    protected override IEnumerableValueProvider GetEnumerableValueProvider(
        BindingSource bindingSource,
        Dictionary<string, StringValues> values,
        CultureInfo culture)
    {
        return new JQueryQueryStringValueProvider(bindingSource, values, culture);
    }

    [Fact]
    public void Filter_ExcludesItself()
    {
        // Arrange
        var dictionary = new Dictionary<string, StringValues>();
        var provider = new JQueryQueryStringValueProvider(
            BindingSource.Form,
            dictionary,
            CultureInfo.CurrentCulture);

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
}
