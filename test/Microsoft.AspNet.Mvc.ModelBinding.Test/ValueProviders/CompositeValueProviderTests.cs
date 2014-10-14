// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CompositeValueProviderTests
    {
        public static IEnumerable<object[]> RegisteredAsMarkerClasses
        {
            get
            {
                yield return new object[] { new TestValueBinderMarker() };
                yield return new object[] { new DerivedValueBinder() };
            }
        }

        [Theory]
        [MemberData(nameof(RegisteredAsMarkerClasses))]
        public void FilterReturnsItself_ForAnyClassRegisteredAsGenericParam(IValueBinderMarker binderMarker)
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var unrelatedMarker = new UnrelatedValueBinderMarker();
            var valueProvider1 = GetMockValueProvider(binderMarker);
            var valueProvider2 = GetMockValueProvider(unrelatedMarker);
            var provider = new CompositeValueProvider(new List<IValueProvider>() { valueProvider1.Object, valueProvider2.Object });

            // Act
            var result = provider.Filter(binderMarker);

            // Assert
            var valueProvider = Assert.IsType<CompositeValueProvider>(result);
            var filteredProvider = Assert.Single(valueProvider);

            // should not be unrelated marker.
            Assert.Same(valueProvider1.Object, filteredProvider);
        }

        private Mock<IMarkerAwareValueProvider> GetMockValueProvider(IValueBinderMarker marker)
        {
            var valueProvider = new Mock<IMarkerAwareValueProvider>();
            valueProvider.Setup(o => o.Filter(marker))
                         .Returns(valueProvider.Object);
            return valueProvider;
        }
        private class TestValueBinderMarker : IValueBinderMarker
        {
        }

        private class DerivedValueBinder : TestValueBinderMarker
        {
        }

        private class UnrelatedValueBinderMarker : IValueBinderMarker
        {
        }
    }
}
#endif