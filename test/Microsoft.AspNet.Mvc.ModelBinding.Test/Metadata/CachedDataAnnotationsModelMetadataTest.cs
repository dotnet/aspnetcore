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
            Assert.False(metadata.HideSurroundingHtml);
            Assert.True(metadata.IsComplexType);
            Assert.False(metadata.IsReadOnly);
            Assert.False(metadata.IsRequired);
            Assert.True(metadata.ShowForDisplay);
            Assert.True(metadata.ShowForEdit);

            Assert.Null(metadata.DataTypeName);
            Assert.Null(metadata.Description);
            Assert.Null(metadata.DisplayFormatString);
            Assert.Null(metadata.DisplayName);
            Assert.Null(metadata.EditFormatString);
            Assert.Null(metadata.NullDisplayText);
            Assert.Null(metadata.SimpleDisplayText);
            Assert.Null(metadata.TemplateHint);

            Assert.Equal(ModelMetadata.DefaultOrder, metadata.Order);
        }

        public static TheoryData<Attribute, Func<ModelMetadata, string>> ExpectedAttributeDataStrings
        {
            get
            {
                return new TheoryData<Attribute, Func<ModelMetadata, string>>
                {
                    {
                        new DisplayAttribute { Description = "value" }, metadata => metadata.Description
                    },
                    {
                        new DisplayAttribute { Name = "value" }, metadata => metadata.DisplayName
                    },
                    {
                        new DisplayColumnAttribute("Property"), metadata => metadata.SimpleDisplayText
                    },
                    {
                        new DisplayFormatAttribute { NullDisplayText = "value" }, metadata => metadata.NullDisplayText
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
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(ClassWithDisplayableColumn),
                propertyName: null,
                attributes: attributes)
            {
                Model = new ClassWithDisplayableColumn { Property = "value" },
            };

            // Act
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
                        metadata => metadata.ConvertEmptyStringToNull,
                        false
                    },
                    {
                        new DisplayFormatAttribute { ConvertEmptyStringToNull = true },
                        metadata => metadata.ConvertEmptyStringToNull,
                        true
                    },
                    {
                        new EditableAttribute(allowEdit: false),
                        metadata => metadata.IsReadOnly,
                        true
                    },
                    {
                        new EditableAttribute(allowEdit: true),
                        metadata => metadata.IsReadOnly,
                        false
                    },
                    {
                        new HiddenInputAttribute { DisplayValue = false },
                        metadata => metadata.HideSurroundingHtml,
                        true
                    },
                    {
                        new HiddenInputAttribute { DisplayValue = true },
                        metadata => metadata.HideSurroundingHtml,
                        false
                    },
                    {
                        new RequiredAttribute(),
                        metadata => metadata.IsRequired,
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
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: attributes);

            // Act
            var result = accessor(metadata);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void HiddenInputWorksOnProperty()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelAccessor: null, modelType: typeof(ClassWithHiddenProperties));
            var property = metadata.Properties.First(m => string.Equals("DirectlyHidden", m.PropertyName));

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        // TODO #1000; enable test once we detect attributes on the property's type
        public void HiddenInputWorksOnPropertyType()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelAccessor: null, modelType: typeof(ClassWithHiddenProperties));
            var property = metadata.Properties.First(m => string.Equals("OfHiddenType", m.PropertyName));

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        private class ClassWithDisplayableColumn
        {
            public string Property { get; set; }
        }

        [HiddenInput(DisplayValue = false)]
        private class HiddenClass
        {
            public string Property { get; set; }
        }

        private class ClassWithHiddenProperties
        {
            [HiddenInput(DisplayValue = false)]
            public string DirectlyHidden { get; set; }

            public HiddenClass OfHiddenType { get; set; }
        }
    }
}