// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ElementalValueProviderTest
    {
        [Theory]
        [InlineData("MyProperty", "MyProperty")]
        [InlineData("MyProperty.SubProperty", "MyProperty")]
        [InlineData("MyProperty[0]", "MyProperty")]
        public async Task ContainsPrefixAsync_ReturnsTrue_IfElementNameStartsWithPrefix(string elementName, 
                                                                                        string prefix)
        {
            // Arrange
            var culture = new CultureInfo("en-US");
            var elementalValueProvider = new ElementalValueProvider(elementName,
                                                                    new object(),
                                                                    culture);

            // Act
            var containsPrefix = await elementalValueProvider.ContainsPrefixAsync(prefix);

            // Assert
            Assert.True(containsPrefix);
        }

        [Theory]
        [InlineData("MyProperty", "MyProperty1")]
        [InlineData("MyPropertyTest", "MyProperty")]
        [InlineData("Random", "MyProperty")]
        public async Task ContainsPrefixAsync_ReturnsFalse_IfElementCannotSpecifyValuesForPrefix(string elementName, 
                                                                                                 string prefix)
        {
            // Arrange
            var culture = new CultureInfo("en-US");
            var elementalValueProvider = new ElementalValueProvider(elementName,
                                                                    new object(),
                                                                    culture);

            // Act
            var containsPrefix = await elementalValueProvider.ContainsPrefixAsync(prefix);

            // Assert
            Assert.False(containsPrefix);
        }

        [Fact]
        public async Task GetValueAsync_NameDoesNotMatch_ReturnsNull()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var rawValue = new DateTime(2001, 1, 2);
            var valueProvider = new ElementalValueProvider("foo", rawValue, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync("bar");

            // Assert
            Assert.Null(vpResult);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("FOO")]
        [InlineData("FoO")]
        public async Task GetValueAsync_NameMatches_ReturnsValueProviderResult(string name)
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var rawValue = new DateTime(2001, 1, 2);
            var valueProvider = new ElementalValueProvider("foo", rawValue, culture);

            // Act
            var vpResult = await valueProvider.GetValueAsync(name);

            // Assert
            Assert.NotNull(vpResult);
            Assert.Equal(rawValue, vpResult.RawValue);
            Assert.Equal("02/01/2001 00:00:00", vpResult.AttemptedValue);
            Assert.Equal(culture, vpResult.Culture);
        }
    }
}
