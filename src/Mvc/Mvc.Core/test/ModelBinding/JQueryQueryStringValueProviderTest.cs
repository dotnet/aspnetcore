// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
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
}
