// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class CompositeValueProviderTest : EnumerableValueProviderTest
    {
        [Fact]
        public override void FilterInclude()
        {
            // Arrange
            var provider = GetBindingSourceValueProvider(BindingSource.Query, BackingStore, culture: null);
            var originalProviders = ((CompositeValueProvider)provider).ToArray();
            var bindingSource = new BindingSource(
                BindingSource.Query.Id,
                displayName: null,
                isGreedy: true,
                isFromRequest: true);

            // Act
            var result = provider.Filter(bindingSource);

            // Assert (does not change inner providers)
            var newProvider = Assert.IsType<CompositeValueProvider>(result);
            Assert.Equal(originalProviders, newProvider, ReferenceEqualityComparer.Instance);
        }

        protected override IEnumerableValueProvider GetEnumerableValueProvider(
            BindingSource bindingSource,
            Dictionary<string, StringValues> values,
            CultureInfo culture)
        {
            var emptyValueProvider = new QueryStringValueProvider(bindingSource, new QueryCollection(), culture);
            var valueProvider = new FormValueProvider(bindingSource, new FormCollection(values), culture);

            return new CompositeValueProvider() { emptyValueProvider, valueProvider };
        }

        [Fact]
        public void GetKeysFromPrefixAsync_ReturnsResultFromFirstValueProviderThatReturnsValues()
        {
            // Arrange
            var provider1 = Mock.Of<IValueProvider>();
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "prefix-test", "some-value" },
            };
            var provider2 = new Mock<IEnumerableValueProvider>();
            provider2.Setup(p => p.GetKeysFromPrefix("prefix"))
                     .Returns(dictionary)
                     .Verifiable();
            var provider = new CompositeValueProvider() { provider1, provider2.Object };

            // Act
            var values = provider.GetKeysFromPrefix("prefix");

            // Assert
            var result = Assert.Single(values);
            Assert.Equal("prefix-test", result.Key);
            Assert.Equal("some-value", result.Value);
            provider2.Verify();
        }

        [Fact]
        public void GetKeysFromPrefixAsync_ReturnsEmptyDictionaryIfNoValueProviderReturnsValues()
        {
            // Arrange
            var provider1 = Mock.Of<IValueProvider>();
            var provider2 = Mock.Of<IValueProvider>();
            var provider = new CompositeValueProvider() { provider1, provider2 };

            // Act
            var values = provider.GetKeysFromPrefix("prefix");

            // Assert
            Assert.Empty(values);
        }

        public static IEnumerable<object[]> BinderMetadata
        {
            get
            {
                yield return new object[] { new TestValueProviderMetadata() };
                yield return new object[] { new DerivedValueProviderMetadata() };
            }
        }

        [Theory]
        [MemberData(nameof(BinderMetadata))]
        public void FilterReturnsItself_ForAnyClassRegisteredAsGenericParam(IBindingSourceMetadata metadata)
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var valueProvider1 = GetMockValueProvider("Test");
            var valueProvider2 = GetMockValueProvider("Unrelated");

            var provider = new CompositeValueProvider() { valueProvider1.Object, valueProvider2.Object };

            // Act
            var result = provider.Filter(metadata.BindingSource);

            // Assert
            var valueProvider = Assert.IsType<CompositeValueProvider>(result);
            var filteredProvider = Assert.Single(valueProvider);

            // should not be unrelated metadata.
            Assert.Same(valueProvider1.Object, filteredProvider);
        }

        public static TheoryData<CompositeValueProvider> Filter_ReturnsProviderData
        {
            get
            {
                // None filter themselves out.
                var noneRewrite = new[]
                {
                    GetValueProvider(rewritesKeys: false),
                    GetValueProvider(rewritesKeys: false),
                };
                // None implement IKeyRewriterValueProvider.
                var noneImplement = new[] { GetMockValueProvider("One").Object, GetMockValueProvider("Two").Object };

                return new TheoryData<CompositeValueProvider>
                {
                    // Starts empty
                    new CompositeValueProvider(),

                    new CompositeValueProvider(noneRewrite),
                    new CompositeValueProvider(noneImplement),
                };
            }
        }

        [Theory]
        [MemberData(nameof(Filter_ReturnsProviderData))]
        public void Filter_ReturnsProvider(CompositeValueProvider provider)
        {
            // Arrange
            var originalProviders = provider.ToArray();

            // Act
            var result = provider.Filter();

            // Assert (does not change inner providers)
            var newProvider = Assert.IsType<CompositeValueProvider>(result);
            Assert.Equal(originalProviders, newProvider, ReferenceEqualityComparer.Instance);
        }

        [Fact]
        public void Filter_ReturnsNull()
        {
            // Arrange
            var allRewrite = new[] { GetValueProvider(rewritesKeys: true), GetValueProvider(rewritesKeys: true) };
            var provider = new CompositeValueProvider(allRewrite);

            // Act
            var result = provider.Filter();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Filter_RemovesThoseThatRewrite()
        {
            // Arrange
            var doesNotRewrite1 = GetValueProvider(rewritesKeys: false);
            var doesNotRewrite2 = GetValueProvider(rewritesKeys: false);
            var doesNotImplement1 = GetMockValueProvider("One").Object;
            var doesNotImplement2 = GetMockValueProvider("Two").Object;
            var rewrites1 = GetValueProvider(rewritesKeys: true);
            var rewrites2 = GetValueProvider(rewritesKeys: true);
            var providers = new IValueProvider[]
            {
                doesNotRewrite1,
                doesNotImplement1,
                rewrites1,
                doesNotRewrite2,
                doesNotImplement2,
                rewrites2,
            };
            var expectedProviders = new IValueProvider[]
            {
                doesNotRewrite1,
                doesNotImplement1,
                doesNotRewrite2,
                doesNotImplement2,
            };

            var provider = new CompositeValueProvider(providers);

            // Act
            var result = provider.Filter();

            // Assert
            Assert.NotSame(provider, result);
            var newProvider = Assert.IsType<CompositeValueProvider>(result);
            Assert.Equal(expectedProviders, newProvider, ReferenceEqualityComparer.Instance);
        }

        private static IKeyRewriterValueProvider GetValueProvider(bool rewritesKeys)
        {
            var valueProvider = new Mock<IKeyRewriterValueProvider>(MockBehavior.Strict);
            if (rewritesKeys)
            {
                valueProvider.Setup(vp => vp.Filter()).Returns<IValueProvider>(null);
            }
            else
            {
                valueProvider.Setup(vp => vp.Filter()).Returns(valueProvider.Object);
            }

            return valueProvider.Object;
        }

        private static Mock<IBindingSourceValueProvider> GetMockValueProvider(string bindingSourceId)
        {
            var valueProvider = new Mock<IBindingSourceValueProvider>(MockBehavior.Strict);

            valueProvider
                .Setup(o => o.Filter(It.Is<BindingSource>(s => s.Id == bindingSourceId)))
                .Returns(valueProvider.Object);

            valueProvider
                .Setup(o => o.Filter(It.Is<BindingSource>(s => s.Id != bindingSourceId)))
                .Returns((IBindingSourceValueProvider)null);

            return valueProvider;
        }

        private class TestValueProviderMetadata : IBindingSourceMetadata
        {
            public BindingSource BindingSource
            {
                get
                {
                    return new BindingSource("Test", displayName: null, isGreedy: true, isFromRequest: true);
                }
            }
        }

        private class DerivedValueProviderMetadata : TestValueProviderMetadata
        {
        }

        private class UnrelatedValueBinderMetadata : IBindingSourceMetadata
        {
            public BindingSource BindingSource
            {
                get
                {
                    return new BindingSource("Unrelated", displayName: null, isGreedy: true, isFromRequest: true);
                }
            }
        }
    }
}
