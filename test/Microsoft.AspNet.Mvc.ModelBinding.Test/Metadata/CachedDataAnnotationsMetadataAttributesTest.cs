// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Test the <see cref="CachedDataAnnotationsMetadataAttributes" /> class.
    /// </summary>
    public class CachedDataAnnotationsMetadataAttributesTest
    {
        [Fact]
        public void Constructor_SetsDefaultValuesForAllProperties()
        {
            // Arrange
            var attributes = Enumerable.Empty<Attribute>();

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(attributes);

            // Assert
            Assert.Null(cache.DataType);
            Assert.Null(cache.Display);
            Assert.Null(cache.DisplayColumn);
            Assert.Null(cache.DisplayFormat);
            Assert.Null(cache.Editable);
            Assert.Null(cache.HiddenInput);
            Assert.Null(cache.Required);
            Assert.Null(cache.ScaffoldColumn);
            Assert.Null(cache.BinderMetadata);
            Assert.Null(cache.BinderModelNameProvider);
            Assert.Empty(cache.PropertyBindingPredicateProviders);
        }

        public static TheoryData<object, Func<CachedDataAnnotationsMetadataAttributes, object>>
            ExpectedAttributeData
        {
            get
            {
                return new TheoryData<object, Func<CachedDataAnnotationsMetadataAttributes, object>>
                {
                    { new DataTypeAttribute(DataType.Duration), cache => cache.DataType },
                    { new DisplayAttribute(), cache => cache.Display },
                    { new DisplayColumnAttribute("Property"), cache => cache.DisplayColumn },
                    { new DisplayFormatAttribute(), cache => cache.DisplayFormat },
                    { new EditableAttribute(allowEdit: false), cache => cache.Editable },
                    { new HiddenInputAttribute(), cache => cache.HiddenInput },
                    { new RequiredAttribute(), cache => cache.Required },
                    { new TestBinderMetadata(), cache => cache.BinderMetadata },
                    { new TestModelNameProvider(), cache => cache.BinderModelNameProvider },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExpectedAttributeData))]
        public void Constructor_FindsExpectedAttribute(
            object attribute,
            Func<CachedDataAnnotationsMetadataAttributes, object> accessor)
        {
            // Arrange
            var attributes = new[] { attribute };

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(attributes);
            var result = accessor(cache);

            // Assert
            Assert.Same(attribute, result);
        }

        [Fact]
        public void Constructor_FindsPropertyBindingInfo()
        {
            // Arrange
            var providers = new[] { new TestPredicateProvider(), new TestPredicateProvider() };

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(providers);
            var result = cache.PropertyBindingPredicateProviders.ToArray();

            // Assert
            Assert.Equal(providers.Length, result.Length);
            for (var index = 0; index < providers.Length; index++)
            {
                Assert.Same(providers[index], result[index]);
            }
        }

        [Fact]
        public void Constructor_FindsBinderTypeProviders()
        {
            // Arrange
            var providers = new[] { new TestBinderTypeProvider(), new TestBinderTypeProvider() };

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(providers);
            var result = cache.BinderTypeProviders.ToArray();

            // Assert
            Assert.Equal(providers.Length, result.Length);
            for (var index = 0; index < providers.Length; index++)
            {
                Assert.Same(providers[index], result[index]);
            }
        }

        [Fact]
        public void Constructor_FindsDisplayFormat_FromDataType()
        {
            // Arrange
            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = dataType.DisplayFormat; // Non-null for DataType.Currency.
            var attributes = new[] { dataType, };

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(attributes);
            var result = cache.DisplayFormat;

            // Assert
            Assert.Same(displayFormat, result);
        }

        [Fact]
        public void Constructor_FindsDisplayFormat_OverridingDataType()
        {
            // Arrange
            var dataType = new DataTypeAttribute(DataType.Time); // Has a non-null DisplayFormat.
            var displayFormat = new DisplayFormatAttribute();
            var attributes = new Attribute[] { dataType, displayFormat, };

            // Act
            var cache = new CachedDataAnnotationsMetadataAttributes(attributes);
            var result = cache.DisplayFormat;

            // Assert
            Assert.Same(displayFormat, result);
        }

        private class TestBinderTypeProvider : IBinderTypeProviderMetadata
        {
            public Type BinderType { get; set; }

            public BindingSource BindingSource { get; set; }
        }

        private class TestPredicateProvider : IPropertyBindingPredicateProvider
        {
            public Func<ModelBindingContext, string, bool> PropertyFilter
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}