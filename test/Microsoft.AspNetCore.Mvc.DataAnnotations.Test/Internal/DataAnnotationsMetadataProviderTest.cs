// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
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

                    { new DisplayNameAttribute("DisplayNameValue"), d => d.DisplayName(), "DisplayNameValue"},
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(new object[] { attribute }, null, null));

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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var dataType = new DataTypeAttribute(DataType.Currency);
            var displayFormat = dataType.DisplayFormat; // Non-null for DataType.Currency.

            var attributes = new[] { dataType, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void CreateDisplayMetadata_FindsDisplayFormat_OverridingDataType()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var dataType = new DataTypeAttribute(DataType.Time); // Has a non-null DisplayFormat.
            var displayFormat = new DisplayFormatAttribute() // But these values override the values from DataType
            {
                DataFormatString = "Cool {0}",
            };

            var attributes = new Attribute[] { dataType, displayFormat, };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Same(displayFormat.DataFormatString, context.DisplayMetadata.DisplayFormatString);
        }

        [Fact]
        public void CreateBindingMetadata_EditableAttributeFalse_SetsReadOnlyTrue()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var editable = new EditableAttribute(allowEdit: false);

            var attributes = new Attribute[] { editable };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsReadOnly);
        }

        [Fact]
        public void CreateBindingMetadata_EditableAttributeTrue_SetsReadOnlyFalse()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var editable = new EditableAttribute(allowEdit: true);

            var attributes = new Attribute[] { editable };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsReadOnly);
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_OverridesDisplayNameAttribute()
        {
            // Arrange
            var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());

            var provider = new DataAnnotationsMetadataProvider(
                localizationOptions,
                stringLocalizerFactory: null);

            var displayName = new DisplayNameAttribute("DisplayNameAttributeValue");
            var display = new DisplayAttribute()
            {
                Name = "DisplayAttributeValue"
            };

            var attributes = new Attribute[] { display, displayName };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("DisplayAttributeValue", context.DisplayMetadata.DisplayName());
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_OverridesDisplayNameAttribute_IfNameEmpty()
        {
            // Arrange
            var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());

            var provider = new DataAnnotationsMetadataProvider(
                localizationOptions,
                stringLocalizerFactory: null);

            var displayName = new DisplayNameAttribute("DisplayNameAttributeValue");
            var display = new DisplayAttribute()
            {
                Name = string.Empty
            };

            var attributes = new Attribute[] { display, displayName };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(string.Empty, context.DisplayMetadata.DisplayName());
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_DoesNotOverrideDisplayNameAttribute_IfNameNull()
        {
            // Arrange
            var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());

            var provider = new DataAnnotationsMetadataProvider(
                localizationOptions,
                stringLocalizerFactory: null);

            var displayName = new DisplayNameAttribute("DisplayNameAttributeValue");
            var display = new DisplayAttribute()
            {
                Description = "This is a description"
            };

            var attributes = new Attribute[] { display, displayName };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("DisplayNameAttributeValue", context.DisplayMetadata.DisplayName());
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayNameAttribute_LocalizesDisplayName()
        {
            // Arrange
            var sharedLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            sharedLocalizer
                .Setup(s => s["DisplayNameValue"])
                .Returns(new LocalizedString("DisplayNameValue", "Name from DisplayNameAttribute"));

            var stringLocalizerFactoryMock = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            stringLocalizerFactoryMock
                .Setup(s => s.Create(typeof(EmptyClass)))
                .Returns(() => sharedLocalizer.Object);

            var localizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            localizationOptions.Value.DataAnnotationLocalizerProvider = (type, stringLocalizerFactory) =>
            {
                return stringLocalizerFactory.Create(typeof(EmptyClass));
            };

            var provider = new DataAnnotationsMetadataProvider(
                localizationOptions,
                stringLocalizerFactory: stringLocalizerFactoryMock.Object);

            var displayName = new DisplayNameAttribute("DisplayNameValue");

            var attributes = new Attribute[] { displayName };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("Name from DisplayNameAttribute", context.DisplayMetadata.DisplayName());
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_NameFromResources_UsesDataAnnotationLocalizerProvider()
        {
            // Arrange
            var sharedLocalizer = new Mock<IStringLocalizer>(MockBehavior.Loose);

            var stringLocalizerFactoryMock = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            stringLocalizerFactoryMock
                .Setup(s => s.Create(typeof(EmptyClass)))
                .Returns(() => sharedLocalizer.Object);

            var options = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            bool dataAnnotationLocalizerProviderWasUsed = false;
            options.Value.DataAnnotationLocalizerProvider = (type, stringLocalizerFactory) =>
            {
                dataAnnotationLocalizerProviderWasUsed = true;
                return stringLocalizerFactory.Create(typeof(EmptyClass));
            };

            var provider = new DataAnnotationsMetadataProvider(options, stringLocalizerFactoryMock.Object);

            var display = new DisplayAttribute()
            {
                Name = "DisplayName"
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);
            context.DisplayMetadata.DisplayName();

            // Assert
            Assert.True(dataAnnotationLocalizerProviderWasUsed, "DataAnnotationLocalizerProvider wasn't used by DisplayMetadata");
        }

        // This is IMPORTANT. Product code needs to use GetName() instead of .Name. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_NameFromResources_NullLocalizer()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

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
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("name from resources", context.DisplayMetadata.DisplayName());
        }

        // This is IMPORTANT. Product code needs to use GetName() instead of .Name. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_NameFromResources_WithLocalizer()
        {
            // Arrange
            // Nothing on stringLocalizer should be called
            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            var stringLocalizerFactory = new Mock<IStringLocalizerFactory>();
            stringLocalizerFactory
                .Setup(s => s.Create(It.IsAny<Type>()))
                .Returns(() => stringLocalizer.Object);
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory.Object);

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
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("name from resources", context.DisplayMetadata.DisplayName());
        }

        // This is IMPORTANT. Product code needs to use GetDescription() instead of .Description. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_DescriptionFromResources_WithLocalizer()
        {
            // Arrange
            // Nothing on stringLocalizer should be called
            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            var stringLocalizerFactory = new Mock<IStringLocalizerFactory>();
            stringLocalizerFactory
                .Setup(s => s.Create(It.IsAny<Type>()))
                .Returns(() => stringLocalizer.Object);
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory.Object);

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
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("description from resources", context.DisplayMetadata.Description());
        }

        // This is IMPORTANT. Product code needs to use GetDescription() instead of .Description. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_DescriptionFromResources_NullLocalizer()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

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
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("description from resources", context.DisplayMetadata.Description());
        }

        // This is IMPORTANT. Product code needs to use GetPrompt() instead of .Prompt. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_PromptFromResources_WithLocalizer()
        {
            // Arrange
            // Nothing on stringLocalizer should be called
            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            var stringLocalizerFactory = new Mock<IStringLocalizerFactory>();
            stringLocalizerFactory
                .Setup(s => s.Create(It.IsAny<Type>()))
                .Returns(() => stringLocalizer.Object);
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory.Object);

            var display = new DisplayAttribute()
            {
#if USE_REAL_RESOURCES
                Prompt = nameof(Test.Resources.DisplayAttribute_Prompt),
                ResourceType = typeof(Test.Resources),
#else
                Prompt = nameof(DataAnnotations.Test.Resources.DisplayAttribute_Prompt),
                ResourceType = typeof(TestResources),
#endif
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("prompt from resources", context.DisplayMetadata.Placeholder());
        }

        // This is IMPORTANT. Product code needs to use GetPrompt() instead of .Prompt. It's easy to regress.
        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_PromptFromResources_NullLocalizer()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var display = new DisplayAttribute()
            {
#if USE_REAL_RESOURCES
                Prompt = nameof(Test.Resources.DisplayAttribute_Prompt),
                ResourceType = typeof(Test.Resources),
#else
                Prompt = nameof(DataAnnotations.Test.Resources.DisplayAttribute_Prompt),
                ResourceType = typeof(TestResources),
#endif
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal("prompt from resources", context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayAttribute_LocalizeProperties()
        {
            // Arrange
            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            stringLocalizer
                .Setup(s => s["Model_Name"])
                .Returns(() => new LocalizedString("Model_Name", "name from localizer " + CultureInfo.CurrentCulture));
            stringLocalizer
                .Setup(s => s["Model_Description"])
                .Returns(() => new LocalizedString("Model_Description", "description from localizer " + CultureInfo.CurrentCulture));
            stringLocalizer
                .Setup(s => s["Model_Prompt"])
                .Returns(() => new LocalizedString("Model_Prompt", "prompt from localizer " + CultureInfo.CurrentCulture));

            var stringLocalizerFactoryMock = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            stringLocalizerFactoryMock
                .Setup(f => f.Create(It.IsAny<Type>()))
                .Returns(stringLocalizer.Object);

            var options = Options.Create(new MvcDataAnnotationsLocalizationOptions());
            options.Value.DataAnnotationLocalizerProvider = (type, stringLocalizerFactory) =>
            {
                return stringLocalizerFactory.Create(type);
            };

            var provider = new DataAnnotationsMetadataProvider(
                options,
                stringLocalizerFactoryMock.Object);

            var display = new DisplayAttribute()
            {
                Name = "Model_Name",
                Description = "Model_Description",
                Prompt = "Model_Prompt"
            };

            var attributes = new Attribute[] { display };
            var key = ModelMetadataIdentity.ForType(typeof(DataAnnotationsMetadataProviderTest));
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            using (new CultureReplacer("en-US", "en-US"))
            {
                Assert.Equal("name from localizer en-US", context.DisplayMetadata.DisplayName());
                Assert.Equal("description from localizer en-US", context.DisplayMetadata.Description());
                Assert.Equal("prompt from localizer en-US", context.DisplayMetadata.Placeholder());
            }
            using (new CultureReplacer("fr-FR", "fr-FR"))
            {
                Assert.Equal("name from localizer fr-FR", context.DisplayMetadata.DisplayName());
                Assert.Equal("description from localizer fr-FR", context.DisplayMetadata.Description());
                Assert.Equal("prompt from localizer fr-FR", context.DisplayMetadata.Placeholder());
            }
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            // This assertion does *not* require entry orders to match.
            Assert.Equal(expectedDictionary, context.DisplayMetadata.EnumNamesAndValues);
        }

        [Fact]
        public void CreateDisplayMetadata_DisplayName_LocalizeWithStringLocalizer()
        {
            // Arrange
            var expectedKeyValuePairs = new List<KeyValuePair<EnumGroupAndName, string>>
            {
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Zero", string.Empty), "0"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayNames.One)), "1"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "dos value"), "2"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "tres value"), "3"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, "name from resources"), "-2"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName("Negatives", "menos uno value"), "-1"),
            };

            var type = typeof(EnumWithDisplayNames);
            var attributes = new object[0];

            var key = ModelMetadataIdentity.ForType(type);
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            stringLocalizer
                .Setup(s => s[It.IsAny<string>()])
                .Returns<string>((index) => new LocalizedString(index, index + " value"));

            var stringLocalizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            stringLocalizerFactory
                .Setup(f => f.Create(It.IsAny<Type>()))
                .Returns(stringLocalizer.Object);

            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory.Object);

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(
                expectedKeyValuePairs,
                context.DisplayMetadata.EnumGroupedDisplayNamesAndValues,
                KVPEnumGroupAndNameComparer.Instance);
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(type);
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(
                expectedKeyValuePairs,
                context.DisplayMetadata.EnumGroupedDisplayNamesAndValues,
                KVPEnumGroupAndNameComparer.Instance);
        }

        [Fact]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_ReflectsDisplayAttributeOrder()
        {
            // Arrange
            var expectedKeyValuePairs = new List<KeyValuePair<EnumGroupAndName, string>>
            {
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayOrder.Three)), "2"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayOrder.Two)), "1"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayOrder.One)), "0"),
                new KeyValuePair<EnumGroupAndName, string>(new EnumGroupAndName(string.Empty, nameof(EnumWithDisplayOrder.Null)), "3"),
            };

            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var key = ModelMetadataIdentity.ForType(typeof(EnumWithDisplayOrder));
            var attributes = new object[0];
            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateDisplayMetadata(context);

            // Assert
            Assert.Equal(
                expectedKeyValuePairs,
                context.DisplayMetadata.EnumGroupedDisplayNamesAndValues,
                KVPEnumGroupAndNameComparer.Instance);
        }

        [Fact]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_NameWithNoIStringLocalizerAndNoResourceType()
        {
            // Arrange & Act
            var enumNameAndGroup = GetLocalizedEnumGroupedDisplayNamesAndValues(useStringLocalizer: false);

            // Assert
            var groupTwo = Assert.Single(enumNameAndGroup, e => e.Value.Equals("2", StringComparison.Ordinal));

            using (new CultureReplacer("en-US", "en-US"))
            {
                Assert.Equal("Loc_Two_Name", groupTwo.Key.Name);
            }

            using (new CultureReplacer("fr-FR", "fr-FR"))
            {
                Assert.Equal("Loc_Two_Name", groupTwo.Key.Name);
            }
        }

        [Fact]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_NameWithIStringLocalizerAndNoResourceType()
        {
            // Arrange & Act
            var enumNameAndGroup = GetLocalizedEnumGroupedDisplayNamesAndValues(useStringLocalizer: true);

            // Assert
            var groupTwo = Assert.Single(enumNameAndGroup, e => e.Value.Equals("2", StringComparison.Ordinal));

            using (new CultureReplacer("en-US", "en-US"))
            {
                Assert.Equal("Loc_Two_Name en-US", groupTwo.Key.Name);
            }

            using (new CultureReplacer("fr-FR", "fr-FR"))
            {
                Assert.Equal("Loc_Two_Name fr-FR", groupTwo.Key.Name);
            }
        }

        [Fact]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_NameWithNoIStringLocalizerAndResourceType()
        {
            // Arrange & Act
            var enumNameAndGroup = GetLocalizedEnumGroupedDisplayNamesAndValues(useStringLocalizer: false);

            // Assert
            var groupThree = Assert.Single(enumNameAndGroup, e => e.Value.Equals("3", StringComparison.Ordinal));

            using (new CultureReplacer("en-US", "en-US"))
            {
                Assert.Equal("type three name en-US", groupThree.Key.Name);
            }

            using (new CultureReplacer("fr-FR", "fr-FR"))
            {
                Assert.Equal("type three name fr-FR", groupThree.Key.Name);
            }
        }

        [Fact]
        public void CreateDisplayMetadata_EnumGroupedDisplayNamesAndValues_NameWithIStringLocalizerAndResourceType()
        {
            // Arrange & Act
            var enumNameAndGroup = GetLocalizedEnumGroupedDisplayNamesAndValues(useStringLocalizer: true);

            var groupThree = Assert.Single(enumNameAndGroup, e => e.Value.Equals("3", StringComparison.Ordinal));

            // Assert
            using (new CultureReplacer("en-US", "en-US"))
            {
                Assert.Equal("type three name en-US", groupThree.Key.Name);
            }

            using (new CultureReplacer("fr-FR", "fr-FR"))
            {
                Assert.Equal("type three name fr-FR", groupThree.Key.Name);
            }
        }

        [Fact]
        public void CreateValidationMetadata_RequiredAttribute_SetsIsRequiredToTrue()
        {
            // Arrange
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var required = new RequiredAttribute();

            var attributes = new Attribute[] { required };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));

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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var attributes = new Attribute[] { };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var attributes = new Attribute[] { new RequiredAttribute() };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var attributes = new Attribute[] { };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));
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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var attribute = new TestValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));

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
            var provider = new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                stringLocalizerFactory: null);

            var attribute = new TestValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));
            context.ValidationMetadata.ValidatorMetadata.Add(attribute);

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        private IEnumerable<KeyValuePair<EnumGroupAndName, string>> GetLocalizedEnumGroupedDisplayNamesAndValues(
            bool useStringLocalizer)
        {
            var provider = CreateIStringLocalizerProvider(useStringLocalizer);

            var key = ModelMetadataIdentity.ForType(typeof(EnumWithLocalizedDisplayNames));
            var attributes = new object[0];

            var context = new DisplayMetadataProviderContext(key, new ModelAttributes(attributes, null, null));
            provider.CreateDisplayMetadata(context);

            return context.DisplayMetadata.EnumGroupedDisplayNamesAndValues;
        }

        private DataAnnotationsMetadataProvider CreateIStringLocalizerProvider(bool useStringLocalizer)
        {
            var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
            stringLocalizer
                .Setup(loc => loc[It.IsAny<string>()])
                .Returns<string>((k =>
                {
                    return new LocalizedString(k, $"{k} {CultureInfo.CurrentCulture}");
                }));

            var stringLocalizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            stringLocalizerFactory
                .Setup(factory => factory.Create(typeof(EnumWithLocalizedDisplayNames)))
                .Returns(stringLocalizer.Object);

            return new DataAnnotationsMetadataProvider(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                useStringLocalizer ? stringLocalizerFactory.Object : null);
        }

        private class KVPEnumGroupAndNameComparer : IEqualityComparer<KeyValuePair<EnumGroupAndName, string>>
        {
            public static readonly IEqualityComparer<KeyValuePair<EnumGroupAndName, string>> Instance = new KVPEnumGroupAndNameComparer();

            private KVPEnumGroupAndNameComparer()
            {
            }

            public bool Equals(KeyValuePair<EnumGroupAndName, string> x, KeyValuePair<EnumGroupAndName, string> y)
            {
                using (new CultureReplacer(string.Empty, string.Empty))
                {
                    return x.Key.Name.Equals(y.Key.Name, StringComparison.Ordinal)
                        && x.Key.Group.Equals(y.Key.Group, StringComparison.Ordinal);
                }
            }

            public int GetHashCode(KeyValuePair<EnumGroupAndName, string> obj)
            {
                using (new CultureReplacer(string.Empty, string.Empty))
                {
                    var hashcode = HashCodeCombiner.Start();

                    hashcode.Add(obj.Key.Name);
                    hashcode.Add(obj.Key.Group);

                    return hashcode.CombinedHash;
                }
            }
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

        private enum EnumWithLocalizedDisplayNames
        {
            [Display(Name = "Loc_Two_Name")]
            Two = 2,
            [Display(Name = nameof(TestResources.Type_Three_Name), ResourceType = typeof(TestResources))]
            Three = 3
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

        private enum EnumWithDisplayOrder
        {
            [Display(Order = 3)]
            One,

            [Display(Order = 2)]
            Two,

            [Display(Order = 1)]
            Three,

            Null,
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