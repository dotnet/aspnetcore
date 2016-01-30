// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    public class DataAnnotationsMetadataProviderTest
    {
        // Includes attributes with a 'simple' effect on display details.
        public static TheoryData<object, Func<DisplayMetadata, object>, object> DisplayDetailsData
        {
            get
            {
                return new TheoryData<object, Func<DisplayMetadata, object>, object>
                {
                    { new DataTypeAttribute(DataType.Duration), d => d.DataTypeName, DataType.Duration.ToString() },

                    { new DisplayAttribute() { Description = "d" }, d => d.Description(), "d" },
                    { new DisplayAttribute() { Name = "DN" }, d => d.DisplayName(), "DN" },
                    { new DisplayAttribute() { Order = 3 }, d => d.Order, 3 },
                    { new DisplayAttribute() { Prompt = "Enter Value" }, d => d.Placeholder(), "Enter Value" },

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
        public void CreateDisplayMetadata_SimpleAttributes(
            object attribute,
            Func<DisplayMetadata, object> accessor,
            object expected)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(new object[] { attribute }));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            var value = accessor(context.DisplayMetadata);
            Assert.Equal(expected, value);
        }

        [Fact]
        public void CreateDisplayMetadata_FindsDisplayFormat_FromDataType()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = dataType.DisplayFormat; // Non-null for DataType.Currency.

            var attributes = new[] { dataType, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void CreateDisplayMetadata_FindsDisplayFormat_OverridingDataType()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var dataType = new DataTypeAttribute(DataType.Time); // Has a non-null DisplayFormat.
            var displayFormat = new DisplayFormatAttribute() // But these values override the values from DataType
            {
                DataFormatString = "Cool {0}",
            };

            var attributes = new Attribute[] { dataType, displayFormat, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void CreateBindingMetadata_EditableAttributeFalse_SetsReadOnlyTrue()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var editable = new EditableAttribute(allowEdit: false);

            var attributes = new Attribute[] { editable };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsReadOnly);
        }

        [Fact]
        public void CreateBindingMetadata_EditableAttributeTrue_SetsReadOnlyFalse()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var editable = new EditableAttribute(allowEdit: true);

            var attributes = new Attribute[] { editable };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsReadOnly);
        }


        // This is IMPORTANT. Product code needs to use GetName() instead of .Name. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_NameFromResources()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var display = new DisplayAttribute()
            {
#if USE_REAL_RESOURCES
                Name = nameof(Test.Resources.DisplayAttribute_Name),
                ResourceType = typeof(Test.Resources),
#else
                Name = nameof(DataAnnotations.Test.Resources.DisplayAttribute_Name),
                ResourceType = typeof(TestResources),
#endif
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("name from resources", context.DisplayMetadata.DisplayName());
        }

        // This is IMPORTANT. Product code needs to use GetDescription() instead of .Description. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_DescriptionFromResources()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var display = new DisplayAttribute()
            {
#if USE_REAL_RESOURCES
                Description = nameof(Test.Resources.DisplayAttribute_Description),
                ResourceType = typeof(Test.Resources),
#else
                Description = nameof(DataAnnotations.Test.Resources.DisplayAttribute_Description),
                ResourceType = typeof(TestResources),
#endif
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("description from resources", context.DisplayMetadata.Description());
        }

        [Theory]
        [InlineData(typeof(EmptyClass), false)]
        [InlineData(typeof(ClassWithFields), false)]
        [InlineData(typeof(ClassWithProperties), false)]
        [InlineData(typeof(EmptyEnum), true)]
        [InlineData(typeof(EmptyEnum?), true)]
        [InlineData(typeof(EnumWithDisplayNames), true)]
        [InlineData(typeof(EnumWithDisplayNames?), true)]
        [InlineData(typeof(EnumWithDuplicates), true)]
        [InlineData(typeof(EnumWithDuplicates?), true)]
        [InlineData(typeof(EnumWithFlags), true)]
        [InlineData(typeof(EnumWithFlags?), true)]
        [InlineData(typeof(EnumWithFields), true)]
        [InlineData(typeof(EnumWithFields?), true)]
        [InlineData(typeof(EmptyStruct), false)]
        [InlineData(typeof(StructWithFields), false)]
        [InlineData(typeof(StructWithFields?), false)]
        [InlineData(typeof(StructWithProperties), false)]
        public void CreateDisplayMetadata_IsEnum_ReflectsModelType(Type type, bool expectedIsEnum)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(expectedIsEnum, context.DisplayMetadata.IsEnum);
        }

        [Theory]
        [InlineData(typeof(EmptyClass), false)]
        [InlineData(typeof(ClassWithFields), false)]
        [InlineData(typeof(ClassWithProperties), false)]
        [InlineData(typeof(EmptyEnum), false)]
        [InlineData(typeof(EmptyEnum?), false)]
        [InlineData(typeof(EnumWithDisplayNames), false)]
        [InlineData(typeof(EnumWithDisplayNames?), false)]
        [InlineData(typeof(EnumWithDuplicates), false)]
        [InlineData(typeof(EnumWithDuplicates?), false)]
        [InlineData(typeof(EnumWithFlags), true)]
        [InlineData(typeof(EnumWithFlags?), true)]
        [InlineData(typeof(EnumWithFields), false)]
        [InlineData(typeof(EnumWithFields?), false)]
        [InlineData(typeof(EmptyStruct), false)]
        [InlineData(typeof(StructWithFields), false)]
        [InlineData(typeof(StructWithFields?), false)]
        [InlineData(typeof(StructWithProperties), false)]
        public void CreateDisplayMetadata_IsFlagsEnum_ReflectsModelType(Type type, bool expectedIsFlagsEnum)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(expectedIsFlagsEnum, context.DisplayMetadata.IsFlagsEnum);
        }

        // Type -> expected EnumNamesAndValues
        public static TheoryData<Type, IReadOnlyDictionary<string, string>> EnumNamesData
        {
            get
            {
                return new TheoryData<Type, IReadOnlyDictionary<string, string>>
                {
                    { typeof(ClassWithFields), null },
                    { typeof(StructWithFields), null },
                    { typeof(StructWithFields?), null },
                    { typeof(EmptyEnum), new Dictionary<string, string>() },
                    { typeof(EmptyEnum?), new Dictionary<string, string>() },
                    {
                        typeof(EnumWithDisplayNames),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithDisplayNames.MinusTwo), "-2" },
                            { nameof(EnumWithDisplayNames.MinusOne), "-1" },
                            { nameof(EnumWithDisplayNames.Zero), "0" },
                            { nameof(EnumWithDisplayNames.One), "1" },
                            { nameof(EnumWithDisplayNames.Two), "2" },
                            { nameof(EnumWithDisplayNames.Three), "3" },
                        }
                    },
                    {
                        typeof(EnumWithDisplayNames?),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithDisplayNames.MinusTwo), "-2" },
                            { nameof(EnumWithDisplayNames.MinusOne), "-1" },
                            { nameof(EnumWithDisplayNames.Zero), "0" },
                            { nameof(EnumWithDisplayNames.One), "1" },
                            { nameof(EnumWithDisplayNames.Two), "2" },
                            { nameof(EnumWithDisplayNames.Three), "3" },
                        }
                    },
                    {
                        typeof(EnumWithDuplicates),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithDuplicates.Zero), "0" },
                            { nameof(EnumWithDuplicates.None), "0" },
                            { nameof(EnumWithDuplicates.One), "1" },
                            { nameof(EnumWithDuplicates.Two), "2" },
                            { nameof(EnumWithDuplicates.Duece), "2" },
                            { nameof(EnumWithDuplicates.Three), "3" },
                            { nameof(EnumWithDuplicates.MoreThanTwo), "3" },
                        }
                    },
                    {
                        typeof(EnumWithDuplicates?),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithDuplicates.Zero), "0" },
                            { nameof(EnumWithDuplicates.None), "0" },
                            { nameof(EnumWithDuplicates.One), "1" },
                            { nameof(EnumWithDuplicates.Two), "2" },
                            { nameof(EnumWithDuplicates.Duece), "2" },
                            { nameof(EnumWithDuplicates.Three), "3" },
                            { nameof(EnumWithDuplicates.MoreThanTwo), "3" },
                        }
                    },
                    {
                        typeof(EnumWithFlags),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithFlags.All), "-1" },
                            { nameof(EnumWithFlags.Zero), "0" },
                            { nameof(EnumWithFlags.One), "1" },
                            { nameof(EnumWithFlags.Two), "2" },
                            { nameof(EnumWithFlags.Four), "4" },
                        }
                    },
                    {
                        typeof(EnumWithFlags?),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithFlags.All), "-1" },
                            { nameof(EnumWithFlags.Zero), "0" },
                            { nameof(EnumWithFlags.One), "1" },
                            { nameof(EnumWithFlags.Two), "2" },
                            { nameof(EnumWithFlags.Four), "4" },
                        }
                    },
                    {
                        typeof(EnumWithFields),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithFields.MinusTwo), "-2" },
                            { nameof(EnumWithFields.MinusOne), "-1" },
                            { nameof(EnumWithFields.Zero), "0" },
                            { nameof(EnumWithFields.One), "1" },
                            { nameof(EnumWithFields.Two), "2" },
                            { nameof(EnumWithFields.Three), "3" },
                        }
                    },
                    {
                        typeof(EnumWithFields?),
                        new Dictionary<string, string>
                        {
                            { nameof(EnumWithFields.MinusTwo), "-2" },
                            { nameof(EnumWithFields.MinusOne), "-1" },
                            { nameof(EnumWithFields.Zero), "0" },
                            { nameof(EnumWithFields.One), "1" },
                            { nameof(EnumWithFields.Two), "2" },
                            { nameof(EnumWithFields.Three), "3" },
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EnumNamesData))]
        public void CreateDisplayMetadata_EnumNamesAndValues_ReflectsModelType(
            Type type,
            IReadOnlyDictionary<string, string> expectedDictionary)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            // This assertion does *not* require entry orders to match.
            Assert.Equal(expectedDictionary, context.DisplayMetadata.EnumNamesAndValues);
        }

        // Type -> expected EnumDisplayNamesAndValues
        public static TheoryData<Type, IEnumerable<KeyValuePair<EnumGroupAndName, string>>> EnumDisplayNamesData
        {
            get
            {
                return new TheoryData<Type, IEnumerable<KeyValuePair<EnumGroupAndName, string>>>
                {
                    { typeof(ClassWithFields), null },
                    { typeof(StructWithFields), null },
                    { typeof(EmptyEnum), new List<KeyValuePair<EnumGroupAndName, string>>() },
                    { typeof(EmptyEnum?), new List<KeyValuePair<EnumGroupAndName, string>>() },
                    {
                        typeof(EnumWithDisplayNames),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Zero", string.Empty), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayNames.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "dos"), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "tres"), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "name from resources"), "-2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Negatives", "menos uno"), "-1"),
                        }
                    },
                    {
                        typeof(EnumWithDisplayNames?),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Zero", string.Empty), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayNames.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "dos"), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "tres"), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "name from resources"), "-2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Negatives", "menos uno"), "-1"),
                        }
                    },
                    {
                        // Note order duplicates appear cannot be inferred easily e.g. does not match the source.
                        // Zero is before None but Two is before Duece in the class below.
                        typeof(EnumWithDuplicates),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.None)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Duece)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.MoreThanTwo)), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Three)), "3"),
                        }
                    },
                    {
                        typeof(EnumWithDuplicates?),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.None)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Duece)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.MoreThanTwo)), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDuplicates.Three)), "3"),
                        }
                    },
                    {
                        typeof(EnumWithFlags),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Four)), "4"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.All)), "-1"),
                        }
                    },
                    {
                        typeof(EnumWithFlags?),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.Four)), "4"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFlags.All)), "-1"),
                        }
                    },
                    {
                        typeof(EnumWithFields),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Three)), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.MinusTwo)), "-2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.MinusOne)), "-1"),
                        }
                    },
                    {
                        typeof(EnumWithFields?),
                        new List<KeyValuePair<EnumGroupAndName, string>>
                        {
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Zero)), "0"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.One)), "1"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Two)), "2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.Three)), "3"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.MinusTwo)), "-2"),
                            new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithFields.MinusOne)), "-1"),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EnumDisplayNamesData))]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_ReflectsModelType(
            Type type,
            IEnumerable<KeyValuePair<EnumGroupAndName, string>> expectedKeyValuePairs)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            // OrderBy is used because the order of the results may very depending on the platform / client.
            Assert.Equal(
                expectedKeyValuePairs?.OrderBy(item => item.Key.Group, StringComparer.Ordinal)
                .ThenBy(item => item.Key.Name, StringComparer.Ordinal),
                context.DisplayMetadata.EnumGroupedDisplayNamesAndValues?.OrderBy(item => item.Key.Group, StringComparer.Ordinal)
                .ThenBy(item => item.Key.Name, StringComparer.Ordinal));
        }

        [Fact]
        public void CreateValidationMetadata_RequiredAttribute_SetsIsRequiredToTrue()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var required = new RequiredAttribute();

            var attributes = new Attribute[] { required };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.True(context.ValidationMetadata.IsRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void CreateValidationMetadata_NoRequiredAttribute_IsRequiredLeftAlone(bool? initialValue)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var attributes = new Attribute[] { };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));
            context.ValidationMetadata.IsRequired = initialValue;

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.ValidationMetadata.IsRequired);
        }

        // [Required] has no effect on IsBindingRequired
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateBindingMetadata_RequiredAttribute_IsBindingRequiredLeftAlone(bool initialValue)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var attributes = new Attribute[] { new RequiredAttribute() };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));
            context.BindingMetadata.IsBindingRequired = initialValue;

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void CreateBindingDetails_NoEditableAttribute_IsReadOnlyLeftAlone(bool? initialValue)
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var attributes = new Attribute[] { };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));
            context.BindingMetadata.IsReadOnly = initialValue;

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsReadOnly);
        }

        [Fact]
        public void CreateValidationDetails_ValidatableObject_ReturnsObject()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var attribute = new TestValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        [Fact]
        public void CreateValidationDetails_ValidatableObject_AlreadyInContext_Ignores()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider();

            var attribute = new TestValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));
            context.ValidationMetadata.ValidatorMetadata.Add(attribute);

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        private class TestValidationAttribute : ValidationAttribute, IClientModelValidator
        {
            public void AddValidation(ClientModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class EmptyClass
        {
        }

        private class ClassWithFields
        {
            public const int Zero = 0;

            public const int One = 1;
        }

        private class ClassWithProperties
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private enum EmptyEnum
        {
        }

        private enum EnumWithDisplayNames
        {
            [Display(Name = "tres")]
            Three = 3,

            [Display(Name = "dos")]
            Two = 2,

            // Display attribute exists but does not set Name.
            [Display(ShortName = "uno")]
            One = 1,

            [Display(Name = "", GroupName = "Zero")]
            Zero = 0,

            [Display(Name = "menos uno", GroupName = "Negatives")]
            MinusOne = -1,

#if USE_REAL_RESOURCES
            [Display(Name = nameof(Test.Resources.DisplayAttribute_Name), ResourceType = typeof(Test.Resources))]
#else
            [Display(Name = nameof(TestResources.DisplayAttribute_Name), ResourceType = typeof(TestResources))]
#endif
            MinusTwo = -2,
        }

        private enum EnumWithDuplicates
        {
            Zero = 0,
            One = 1,
            Three = 3,
            MoreThanTwo = 3,
            Two = 2,
            None = 0,
            Duece = 2,
        }

        [Flags]
        private enum EnumWithFlags
        {
            Four = 4,
            Two = 2,
            One = 1,
            Zero = 0,
            All = -1,
        }

        private enum EnumWithFields
        {
            MinusTwo = -2,
            MinusOne = -1,
            Three = 3,
            Two = 2,
            One = 1,
            Zero = 0,
        }

        private struct EmptyStruct
        {
        }

        private struct StructWithFields
        {
            public const int Zero = 0;

            public const int One = 1;
        }

        private struct StructWithProperties
        {
            public StructWithProperties(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; private set; }

            public string Name { get; private set; }
        }
    }
}