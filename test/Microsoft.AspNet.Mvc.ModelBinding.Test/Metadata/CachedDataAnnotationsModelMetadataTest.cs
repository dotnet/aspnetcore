// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Test the <see cref="CachedDataAnnotationsModelMetadata" /> class.
    /// </summary>
    public class CachedDataAnnotationsModelMetadataTest
    {
        [Fact]
        public void Constructor_DefersDefaultsToBaseModelMetadata()
        {
            // Arrange
            var attributes = Enumerable.Empty<Attribute>();
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: attributes);

            // Assert
            Assert.True(metadata.ConvertEmptyStringToNull);
            Assert.False(metadata.IsReadOnly);
            Assert.False(metadata.IsRequired);

            Assert.Null(metadata.Description);
            Assert.Null(metadata.DisplayName);
            Assert.Null(metadata.NullDisplayText);
        }

        public static TheoryData<Attribute, Func<ModelMetadata, string>> ExpectedAttributeDataStrings
        {
            get
            {
                return new TheoryData<Attribute, Func<ModelMetadata, string>>
                {
                    {
                        new DisplayAttribute { Description = "value" },
                        (ModelMetadata metadata) => metadata.Description
                    },
                    {
                        new DisplayAttribute { Name = "value" },
                        (ModelMetadata metadata) => metadata.DisplayName
                    },
                    {
                        new DisplayColumnAttribute("Property"),
                        (ModelMetadata metadata) => metadata.SimpleDisplayText
                    },
                    {
                        new DisplayFormatAttribute { NullDisplayText = "value" },
                        (ModelMetadata metadata) => metadata.NullDisplayText
                    },
                };
            }
        }

        [Theory]
        [MemberData("ExpectedAttributeDataStrings")]
        public void AttributesOverrideMetadataStrings(Attribute attribute, Func<ModelMetadata, string> accessor)
        {
            // Arrange
            var attributes = new[] { attribute };
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(ClassWithDisplayableColumn),
                propertyName: null,
                attributes: attributes)
            {
                Model = new ClassWithDisplayableColumn { Property = "value" },
            };
            var result = accessor(metadata);

            // Assert
            Assert.Equal("value", result);
        }

        public static TheoryData<Attribute, Func<ModelMetadata, bool>, bool> ExpectedAttributeDataBooleans
        {
            get
            {
                return new TheoryData<Attribute, Func<ModelMetadata, bool>, bool>
                {
                    {
                        new DisplayFormatAttribute { ConvertEmptyStringToNull = false },
                        (ModelMetadata metadata) => metadata.ConvertEmptyStringToNull,
                        false
                    },
                    {
                        new DisplayFormatAttribute { ConvertEmptyStringToNull = true },
                        (ModelMetadata metadata) => metadata.ConvertEmptyStringToNull,
                        true
                    },
                    {
                        new EditableAttribute(allowEdit: false),
                        (ModelMetadata metadata) => metadata.IsReadOnly,
                        true
                    },
                    {
                        new EditableAttribute(allowEdit: true),
                        (ModelMetadata metadata) => metadata.IsReadOnly,
                        false
                    },
                    {
                        new RequiredAttribute(),
                        (ModelMetadata metadata) => metadata.IsRequired,
                        true
                    },
                };
            }
        }

        [Theory]
        [MemberData("ExpectedAttributeDataBooleans")]
        public void AttributesOverrideMetadataBooleans(
            Attribute attribute,
            Func<ModelMetadata, bool> accessor,
            bool expectedResult)
        {
            // Arrange
            var attributes = new[] { attribute };
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: attributes);
            var result = accessor(metadata);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        private class ClassWithDisplayableColumn
        {
            public string Property { get; set; }
        }
    }
}