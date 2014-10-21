// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CompositeValueProviderTests
    {
        public static IEnumerable<object[]> RegisteredAsMetadataClasses
        {
            get
            {
                yield return new object[] { new TestValueProviderMetadata() };
                yield return new object[] { new DerivedValueBinderMetadata() };
            }
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_ReturnsResultFromFirstValueProviderThatReturnsValues()
        {
            // Arrange
            var provider1 = Mock.Of<IValueProvider>();
            var dictionary =  new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "prefix-test", "some-value" },
            };
            var provider2 = new Mock<IEnumerableValueProvider>();
            provider2.Setup(p => p.GetKeysFromPrefixAsync("prefix"))
                     .Returns(Task.FromResult<IDictionary<string, string>>(dictionary))
                     .Verifiable();
            var provider = new CompositeValueProvider(new[] { provider1, provider2.Object });

            // Act
            var values = await provider.GetKeysFromPrefixAsync("prefix");

            // Assert
            var result = Assert.Single(values);
            Assert.Equal("prefix-test", result.Key);
            Assert.Equal("some-value", result.Value);
            provider2.Verify();
        }

        [Fact]
        public async Task GetKeysFromPrefixAsync_ReturnsEmptyDictionaryIfNoValueProviderReturnsValues()
        {
            // Arrange
            var provider1 = Mock.Of<IValueProvider>();
            var provider2 = Mock.Of<IValueProvider>();
            var provider = new CompositeValueProvider(new[] { provider1, provider2 });

            // Act
            var values = await provider.GetKeysFromPrefixAsync("prefix");

            // Assert
            Assert.Empty(values);
        }

        [Theory]
        [MemberData(nameof(RegisteredAsMetadataClasses))]
        public void FilterReturnsItself_ForAnyClassRegisteredAsGenericParam(IValueProviderMetadata metadata)
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var unrelatedMetadata = new UnrelatedValueBinderMetadata();
            var valueProvider1 = GetMockValueProvider(metadata);
            var valueProvider2 = GetMockValueProvider(unrelatedMetadata);
            var provider = new CompositeValueProvider(new List<IValueProvider>() { valueProvider1.Object, valueProvider2.Object });

            // Act
            var result = provider.Filter(metadata);

            // Assert
            var valueProvider = Assert.IsType<CompositeValueProvider>(result);
            var filteredProvider = Assert.Single(valueProvider);

            // should not be unrelated metadata.
            Assert.Same(valueProvider1.Object, filteredProvider);
        }

        private Mock<IMetadataAwareValueProvider> GetMockValueProvider(IValueProviderMetadata metadata)
        {
            var valueProvider = new Mock<IMetadataAwareValueProvider>();
            valueProvider.Setup(o => o.Filter(metadata))
                         .Returns(valueProvider.Object);
            return valueProvider;
        }
        private class TestValueProviderMetadata : IValueProviderMetadata
        {
        }

        private class DerivedValueBinderMetadata : TestValueProviderMetadata
        {
        }

        private class UnrelatedValueBinderMetadata : IValueProviderMetadata
        {
        }
    }
}
#endif