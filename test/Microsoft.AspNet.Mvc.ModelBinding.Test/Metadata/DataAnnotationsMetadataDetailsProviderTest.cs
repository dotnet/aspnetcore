// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DataAnnotationsMetadataDetailsProviderTest
    {
        // Includes attributes with a 'simple' effect on display details. 
        public static TheoryData<object, Func<DisplayMetadata, object>, object> DisplayDetailsData
        {
            get
            {
                return new TheoryData<object, Func<DisplayMetadata, object>, object>
                {
                    { new DataTypeAttribute(DataType.Duration), d => d.DataTypeName, DataType.Duration.ToString() },

                    { new DisplayAttribute() { Description = "d" }, d => d.Description, "d" },
                    { new DisplayAttribute() { Name = "DN" }, d => d.DisplayName, "DN" },
                    { new DisplayAttribute() { Order = 3 }, d => d.Order, 3 },

                    { new DisplayColumnAttribute("Property"), d => d.SimpleDisplayProperty, "Property" },

                    { new DisplayFormatAttribute() { ConvertEmptyStringToNull = true }, d => d.ConvertEmptyStringToNull, true },
                    { new DisplayFormatAttribute() { DataFormatString = "{0:G}" }, d => d.DisplayFormatString, "{0:G}" },
                    {
                        new DisplayFormatAttribute() { DataFormatString = "{0:G}", ApplyFormatInEditMode = true },
                        d => d.EditFormatString,
                        "{0:G}"
                    },
                    { new DisplayFormatAttribute() { HtmlEncode = false }, d => d.HtmlEncode, false },
                    { new DisplayFormatAttribute() { NullDisplayText = "(null)" }, d => d.NullDisplayText, "(null)" },

                    { new HiddenInputAttribute() { DisplayValue = false }, d => d.HideSurroundingHtml, true },

                    { new ScaffoldColumnAttribute(scaffold: false), d => d.ShowForDisplay, false },
                    { new ScaffoldColumnAttribute(scaffold: false), d => d.ShowForEdit, false },

                    { new UIHintAttribute("hintHint"), d => d.TemplateHint, "hintHint" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DisplayDetailsData))]
        public void GetDisplayDetails_SimpleAttributes(
            object attribute, 
            Func<DisplayMetadata, object> accessor, 
            object expected)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new object[] { attribute });

            // Act
            provider.GetDisplayMetadata(context);

            // Assert
            var value = accessor(context.DisplayMetadata);
            Assert.Equal(expected, value);
        }

        [Fact]
        public void GetDisplayDetails_FindsDisplayFormat_FromDataType()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = dataType.DisplayFormat; // Non-null for DataType.Currency.

            var attributes = new[] { dataType, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, attributes);

            // Act
            provider.GetDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void GetDisplayDetails_FindsDisplayFormat_OverridingDataType()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var dataType = new DataTypeAttribute(DataType.Time); // Has a non-null DisplayFormat.
            var displayFormat = new DisplayFormatAttribute() // But these values override the values from DataType
            {
                DataFormatString = "Cool {0}",
            };

            var attributes = new Attribute[] { dataType, displayFormat, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, attributes);

            // Act
            provider.GetDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void GetDisplayDetails_EditableAttribute_SetsReadOnly()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var editable = new EditableAttribute(allowEdit: false);

            var attributes = new Attribute[] { editable };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, attributes);

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(true, context.BindingMetadata.IsReadOnly);
        }

        [Fact]
        public void GetDisplayDetails_RequiredAttribute_SetsRequired()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var required = new RequiredAttribute();

            var attributes = new Attribute[] { required };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, attributes);

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(true, context.BindingMetadata.IsRequired);
        }

        // This is IMPORTANT. Product code needs to use GetName() instead of .Name. It's easy to regress.
        [Fact]
        public void GetDisplayDetails_DisplayAttribute_NameFromResources()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var display = new DisplayAttribute()
            {
                Name = "DisplayAttribute_Name",
                ResourceType = typeof(Test.TestResources),
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, attributes);

            // Act
            provider.GetDisplayMetadata(context);

            // Assert
            Assert.Equal("name from resources", context.DisplayMetadata.DisplayName);
        }

        // This is IMPORTANT. Product code needs to use GetDescription() instead of .Description. It's easy to regress.
        [Fact]
        public void GetDisplayDetails_DisplayAttribute_DescriptionFromResources()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataDetailsProvider();

            var display = new DisplayAttribute()
            {
                Description = "DisplayAttribute_Description",
                ResourceType = typeof(Test.TestResources),
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, attributes);

            // Act
            provider.GetDisplayMetadata(context);

            // Assert
            Assert.Equal("description from resources", context.DisplayMetadata.Description);
        }
    }
}