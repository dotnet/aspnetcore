// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public void Constructor_DefaultsAllPropertiesToNull()
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
        }

        public static TheoryData<Attribute, Func<CachedDataAnnotationsMetadataAttributes, Attribute>>
            ExpectedAttributeData
        {
            get
            {
                return new TheoryData<Attribute, Func<CachedDataAnnotationsMetadataAttributes, Attribute>>
                {
                    { new DataTypeAttribute(DataType.Duration), cache => cache.DataType },
                    { new DisplayAttribute(), cache => cache.Display },
                    { new DisplayColumnAttribute("Property"), cache => cache.DisplayColumn },
                    { new DisplayFormatAttribute(), cache => cache.DisplayFormat },
                    { new EditableAttribute(allowEdit: false), cache => cache.Editable },
                    { new HiddenInputAttribute(), cache => cache.HiddenInput },
                    { new RequiredAttribute(), cache => cache.Required },
                    { new ScaffoldColumnAttribute(scaffold: false), cache => cache.ScaffoldColumn },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExpectedAttributeData))]
        public void Constructor_FindsExpectedAttribute(
            Attribute attribute,
            Func<CachedDataAnnotationsMetadataAttributes, Attribute> accessor)
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
    }
}