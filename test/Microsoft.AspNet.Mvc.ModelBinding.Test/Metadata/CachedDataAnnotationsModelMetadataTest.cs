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
            Assert.False(metadata.HasNonDefaultEditFormat);
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
                        new DataTypeAttribute("value"), metadata => metadata.DataTypeName
                    },
                    {
                        new DataTypeWithCustomDisplayFormat(), metadata => metadata.DisplayFormatString
                    },
                    {
                        new DataTypeWithCustomEditFormat(), metadata => metadata.EditFormatString
                    },
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
                        new DisplayFormatAttribute { DataFormatString = "value" },
                        metadata => metadata.DisplayFormatString
                    },
                    {
                        // DisplayFormatString does not ignore [DisplayFormat] if ApplyFormatInEditMode==true.
                        new DisplayFormatAttribute { ApplyFormatInEditMode = true, DataFormatString = "value" },
                        metadata => metadata.DisplayFormatString
                    },
                    {
                        new DisplayFormatAttribute { ApplyFormatInEditMode = true, DataFormatString = "value" },
                        metadata => metadata.EditFormatString
                    },
                    {
                        new DisplayFormatAttribute { NullDisplayText = "value" }, metadata => metadata.NullDisplayText
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExpectedAttributeDataStrings))]
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
                        // Edit formats from [DataType] subclass affect HasNonDefaultEditFormat.
                        new DataTypeWithCustomEditFormat(),
                        metadata => metadata.HasNonDefaultEditFormat,
                        true
                    },
                    {
                        // Edit formats from [DataType] do not affect HasNonDefaultEditFormat.
                        new DataTypeAttribute(DataType.Date),
                        metadata => metadata.HasNonDefaultEditFormat,
                        false
                    },
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
                        // Changes only to DisplayFormatString do not affect HasNonDefaultEditFormat.
                        new DisplayFormatAttribute { DataFormatString = "value" },
                        metadata => metadata.HasNonDefaultEditFormat,
                        false
                    },
                    {
                        new DisplayFormatAttribute { ApplyFormatInEditMode = true, DataFormatString = "value" },
                        metadata => metadata.HasNonDefaultEditFormat,
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
        [MemberData(nameof(ExpectedAttributeDataBooleans))]
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
        public void DataTypeName_Null_IfHtmlEncodeTrue()
        {
            // Arrange
            var displayFormat = new DisplayFormatAttribute { HtmlEncode = true, };
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: new Attribute[] { displayFormat });

            // Act
            var result = metadata.DataTypeName;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DataTypeName_Html_IfHtmlEncodeFalse()
        {
            // Arrange
            var expected = "Html";
            var displayFormat = new DisplayFormatAttribute { HtmlEncode = false, };
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: new Attribute[] { displayFormat });

            // Act
            var result = metadata.DataTypeName;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DataTypeName_AttributesHaveExpectedPrecedence()
        {
            // Arrange
            var expected = "MultilineText";
            var dataType = new DataTypeAttribute(DataType.MultilineText);
            var displayFormat = new DisplayFormatAttribute { HtmlEncode = false, };
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: new Attribute[] { dataType, displayFormat });

            // Act
            var result = metadata.DataTypeName;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DisplayFormatString_AttributesHaveExpectedPrecedence()
        {
            // Arrange
            var expected = "custom format";
            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = new DisplayFormatAttribute { DataFormatString = expected, };
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: new Attribute[] { dataType, displayFormat });

            // Act
            var result = metadata.DisplayFormatString;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EditFormatString_AttributesHaveExpectedPrecedence()
        {
            // Arrange
            var expected = "custom format";
            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = new DisplayFormatAttribute
            {
                ApplyFormatInEditMode = true,
                DataFormatString = expected,
            };
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: new Attribute[] { dataType, displayFormat });

            // Act
            var result = metadata.EditFormatString;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        private void EditFormatString_DoesNotAffectDisplayFormat()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: Enumerable.Empty<Attribute>());

            // Act
            metadata.EditFormatString = "custom format";

            // Assert
            Assert.Null(metadata.DisplayFormatString);
        }

        [Fact]
        private void DisplayFormatString_DoesNotAffectEditFormat()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = new CachedDataAnnotationsModelMetadata(
                provider,
                containerType: null,
                modelType: typeof(object),
                propertyName: null,
                attributes: Enumerable.Empty<Attribute>());

            // Act
            metadata.DisplayFormatString = "custom format";

            // Assert
            Assert.Null(metadata.EditFormatString);
        }

        private class DataTypeWithCustomDisplayFormat : DataTypeAttribute
        {
            public DataTypeWithCustomDisplayFormat() : base("Custom datatype")
            {
                DisplayFormat = new DisplayFormatAttribute
                {
                    DataFormatString = "value",
                };
            }
        }

        private class DataTypeWithCustomEditFormat : DataTypeAttribute
        {
            public DataTypeWithCustomEditFormat() : base("Custom datatype")
            {
                DisplayFormat = new DisplayFormatAttribute
                {
                    ApplyFormatInEditMode = true,
                    DataFormatString = "value",
                };
            }
        }

        private class ClassWithDisplayableColumn
        {
            public string Property { get; set; }
        }
    }
}