// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

// Integration tests for the default provider configuration.
public class ModelMetadataProviderTest
{
    [Fact]
    public void ModelMetadataProvider_UsesPropertyFilterProviderOnType()
    {
        // Arrange
        var type = typeof(User);

        var provider = CreateProvider();
        var context = new DefaultModelBindingContext();

        var expected = new[] { "IsAdmin", "UserName" };

        // Act
        var metadata = provider.GetMetadataForType(type);

        // Assert
        var propertyFilter = metadata.PropertyFilterProvider.PropertyFilter;

        var matched = new HashSet<string>();
        foreach (var property in metadata.Properties)
        {
            if (propertyFilter(property))
            {
                matched.Add(property.PropertyName);
            }
        }

        Assert.Equal<string>(expected, matched);
    }

    [Fact]
    public void ModelMetadataProvider_ReadsModelNameProperty_ForTypes()
    {
        // Arrange
        var type = typeof(User);
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForType(type);

        // Assert
        Assert.Equal("TypePrefix", metadata.BinderModelName);
    }

    [Fact]
    public void ModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForDisplay()
    {
        // Arrange
        var type = typeof(ScaffoldColumnModel);
        var provider = CreateProvider();

        // Act & Assert
        Assert.True(provider.GetMetadataForProperty(type, "NoAttribute").ShowForDisplay);
        Assert.True(provider.GetMetadataForProperty(type, "ScaffoldColumnTrue").ShowForDisplay);
        Assert.False(provider.GetMetadataForProperty(type, "ScaffoldColumnFalse").ShowForDisplay);
    }

    [Fact]
    public void ModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForEdit()
    {
        // Arrange
        var type = typeof(ScaffoldColumnModel);
        var provider = CreateProvider();

        // Act & Assert
        Assert.True(provider.GetMetadataForProperty(type, "NoAttribute").ShowForEdit);
        Assert.True(provider.GetMetadataForProperty(type, "ScaffoldColumnTrue").ShowForEdit);
        Assert.False(provider.GetMetadataForProperty(type, "ScaffoldColumnFalse").ShowForEdit);
    }

    [Fact]
    public void HiddenInputWorksOnProperty_ForHideSurroundingHtml()
    {
        // Arrange
        var provider = CreateProvider();
        var metadata = provider.GetMetadataForType(modelType: typeof(ClassWithHiddenProperties));
        var property = metadata.Properties["DirectlyHidden"];

        // Act
        var result = property.HideSurroundingHtml;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HiddenInputWorksOnPropertyType_ForHideSurroundingHtml()
    {
        // Arrange
        var provider = CreateProvider();
        var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
        var property = metadata.Properties["OfHiddenType"];

        // Act
        var result = property.HideSurroundingHtml;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HiddenInputWorksOnProperty_ForTemplateHint()
    {
        // Arrange
        var provider = CreateProvider();
        var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
        var property = metadata.Properties["DirectlyHidden"];

        // Act
        var result = property.TemplateHint;

        // Assert
        Assert.Equal("HiddenInput", result);
    }

    [Fact]
    public void HiddenInputWorksOnPropertyType_ForTemplateHint()
    {
        // Arrange
        var provider = CreateProvider();
        var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
        var property = metadata.Properties["OfHiddenType"];

        // Act
        var result = property.TemplateHint;

        // Assert
        Assert.Equal("HiddenInput", result);
    }

    [Fact]
    public void GetMetadataForProperty_WithNoBinderModelName_GetsItFromType()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var propertyMetadata = provider.GetMetadataForProperty(typeof(Person), nameof(Person.Parent));

        // Assert
        Assert.Equal("PersonType", propertyMetadata.BinderModelName);
    }

    [Fact]
    public void GetMetadataForProperty_WithBinderMetadataOnPropertyAndType_GetsMetadataFromProperty()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var propertyMetadata = provider.GetMetadataForProperty(typeof(Person), nameof(Person.GrandParent));

        // Assert
        Assert.Equal("GrandParentProperty", propertyMetadata.BinderModelName);
    }

    public static TheoryData<object, Func<ModelMetadata, string>> ExpectedAttributeDataStrings
    {
        get
        {
            return new TheoryData<object, Func<ModelMetadata, string>>
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
                        new DisplayAttribute { Prompt = "value" }, metadata => metadata.Placeholder
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
                    {
                        new TestModelNameProvider { Name = "value" }, metadata => metadata.BinderModelName
                    },
                    {
                         new UIHintAttribute("value"), metadata => metadata.TemplateHint
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedAttributeDataStrings))]
    public void AttributesOverrideMetadataStrings(object attribute, Func<ModelMetadata, string> accessor)
    {
        // Arrange
        var attributes = new[] { attribute };
        var provider = CreateProvider(attributes);

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = accessor(metadata);

        // Assert
        Assert.Equal("value", result);
    }

    [Fact]
    public void AttributesOverrideMetadataStrings_SimpleDisplayProperty()
    {
        // Arrange
        var attributes = new[] { new DisplayColumnAttribute("Property") };
        var provider = CreateProvider(attributes);

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.SimpleDisplayProperty;

        // Assert
        Assert.Equal("Property", result);
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
                        new DisplayFormatAttribute { HtmlEncode = false },
                        metadata => metadata.HtmlEncode,
                        false
                    },
                    {
                        new DisplayFormatAttribute { HtmlEncode = true },
                        metadata => metadata.HtmlEncode,
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
                        new HiddenInputAttribute(),
                        metadata => string.Equals("HiddenInput", metadata.TemplateHint, StringComparison.Ordinal),
                        true
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
        var provider = CreateProvider(attributes);
        var metadata = provider.GetMetadataForProperty(typeof(CoolUser), nameof(CoolUser.Name));

        // Act
        var result = accessor(metadata);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    public static TheoryData<DisplayAttribute, int> DisplayAttribute_OverridesOrderData
    {
        get
        {
            return new TheoryData<DisplayAttribute, int>
                {
                    {
                        new DisplayAttribute(), ModelMetadata.DefaultOrder
                    },
                    {
                        new DisplayAttribute { Order = int.MinValue }, int.MinValue
                    },
                    {
                        new DisplayAttribute { Order = -100 }, -100
                    },
                    {
                        new DisplayAttribute { Order = -1 }, -1
                    },
                    {
                        new DisplayAttribute { Order = 0 }, 0
                    },
                    {
                        new DisplayAttribute { Order = 1 }, 1
                    },
                    {
                        new DisplayAttribute { Order = 200 }, 200
                    },
                    {
                        new DisplayAttribute { Order = int.MaxValue }, int.MaxValue
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DisplayAttribute_OverridesOrderData))]
    public void DisplayAttribute_OverridesOrder(DisplayAttribute attribute, int expectedOrder)
    {
        // Arrange
        var attributes = new[] { attribute };
        var provider = CreateProvider(attributes);

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.Order;

        // Assert
        Assert.Equal(expectedOrder, result);
    }

    [Fact]
    public void DisplayAttribute_Description()
    {
        // Arrange
        var display = new DisplayAttribute() { Description = "description" };
        var provider = CreateProvider(new[] { display });

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.Description;

        // Assert
        Assert.Equal("description", result);
    }

    [Fact]
    public void DisplayAttribute_PromptAsPlaceholder()
    {
        // Arrange
        var display = new DisplayAttribute() { Prompt = "prompt" };
        var provider = CreateProvider(new[] { display });

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.Placeholder;

        // Assert
        Assert.Equal("prompt", result);
    }

    [Fact]
    public void DisplayName_FromResources_GetsRecomputed()
    {
        // Arrange
        var display = new DisplayAttribute()
        {
            Name = nameof(TestResources.DisplayAttribute_CultureSensitiveName),
            ResourceType = typeof(TestResources),
        };

        var provider = CreateProvider(new[] { display });
        var metadata = provider.GetMetadataForType(typeof(string));

        // Act & Assert
        var cultures = new[] { "fr-FR", "en-US", "en-GB" };
        foreach (var culture in cultures)
        {
            using (new CultureReplacer(uiCulture: culture))
            {
                // Later iterations ensure value is recomputed.
                var result = metadata.DisplayName;
                Assert.Equal("name from resources" + culture, result);
            }
        }
    }

    [Fact]
    public void Description_FromResources_GetsRecomputed()
    {
        // Arrange
        var display = new DisplayAttribute()
        {
            Description = nameof(TestResources.DisplayAttribute_CultureSensitiveDescription),
            ResourceType = typeof(TestResources),
        };

        var provider = CreateProvider(new[] { display });
        var metadata = provider.GetMetadataForType(typeof(string));

        // Act & Assert
        var cultures = new[] { "fr-FR", "en-US", "en-GB" };
        foreach (var culture in cultures)
        {
            using (new CultureReplacer(uiCulture: culture))
            {
                // Later iterations ensure value is recomputed.
                var result = metadata.Description;
                Assert.Equal("description from resources" + culture, result);
            }
        }
    }

    [Fact]
    public void DataTypeName_Null_IfHtmlEncodeTrue()
    {
        // Arrange
        var displayFormat = new DisplayFormatAttribute { HtmlEncode = true, };
        var provider = CreateProvider(new[] { displayFormat });

        var metadata = provider.GetMetadataForType(typeof(string));

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
        var provider = CreateProvider(new[] { displayFormat });

        var metadata = provider.GetMetadataForType(typeof(string));

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
        var provider = CreateProvider(new object[] { dataType, displayFormat });

        var metadata = provider.GetMetadataForType(typeof(string));

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
        var provider = CreateProvider(new object[] { displayFormat, dataType, });

        var metadata = provider.GetMetadataForType(typeof(string));

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

        var provider = CreateProvider(new object[] { displayFormat, dataType, });

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.EditFormatString;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TemplateHint_AttributesHaveExpectedPrecedence()
    {
        // Arrange
        var expected = "this is a hint";
        var hidden = new HiddenInputAttribute();
        var uiHint = new UIHintAttribute(expected);
        var provider = CreateProvider(new object[] { hidden, uiHint, });

        var metadata = provider.GetMetadataForType(typeof(string));

        // Act
        var result = metadata.TemplateHint;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BinderTypeProviders_Null()
    {
        // Arrange
        var binderProviders = new[]
        {
                new TestBinderTypeProvider(),
                new TestBinderTypeProvider(),
            };

        var provider = CreateProvider(binderProviders);

        // Act
        var metadata = provider.GetMetadataForType(typeof(string));

        // Assert
        Assert.Null(metadata.BinderType);
    }

    [Fact]
    public void BinderTypeProviders_Fallback()
    {
        // Arrange
        var attributes = new[]
        {
                new TestBinderTypeProvider(),
                new TestBinderTypeProvider() { BinderType = typeof(ComplexObjectModelBinder) }
            };

        var provider = CreateProvider(attributes);

        // Act
        var metadata = provider.GetMetadataForType(typeof(string));

        // Assert
        Assert.Same(typeof(ComplexObjectModelBinder), metadata.BinderType);
    }

    [Fact]
    public void BinderTypeProviders_FirstAttributeHasPrecedence()
    {
        // Arrange
        var attributes = new[]
        {
                new TestBinderTypeProvider() { BinderType = typeof(ComplexObjectModelBinder) },
                new TestBinderTypeProvider() { BinderType = typeof(SimpleTypeModelBinder) }
            };

        var provider = CreateProvider(attributes);

        // Act
        var metadata = provider.GetMetadataForType(typeof(string));

        // Assert
        Assert.Same(typeof(ComplexObjectModelBinder), metadata.BinderType);
    }

    [Fact]
    public void IsRequired_DefaultsToTrueForValueType()
    {
        // Arrange
        var attributes = new object[]
        {
        };

        var provider = CreateProvider(attributes);

        // Act
        var metadata = provider.GetMetadataForProperty(typeof(string), "Length");

        // Assert
        Assert.True(metadata.IsRequired);
    }

    [Fact]
    public void IsRequired_DefaultsToFalseForReferenceType()
    {
        // Arrange
        var attributes = new object[]
        {
        };

        var provider = CreateProvider(attributes);

        // Act
        var metadata = provider.GetMetadataForProperty(typeof(Person), nameof(Person.Parent));

        // Assert
        Assert.False(metadata.IsRequired);
    }

    [Fact]
    public void IsRequired_WithRequiredAttribute()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = provider.GetMetadataForProperty(
            typeof(RequiredMember),
            nameof(RequiredMember.RequiredProperty));

        // Assert
        Assert.True(metadata.IsRequired);
    }

    [Fact]
    public void IsBindingRequired_DataMemberIsRequiredTrue_SetsIsBindingRequiredToTrue()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ClassWithDataMemberIsRequiredTrue),
            nameof(ClassWithDataMemberIsRequiredTrue.StringProperty));

        // Assert
        Assert.True(metadata.IsBindingRequired);
    }

    [Fact]
    public void IsBindingRequired_DataMemberIsRequiredFalse_False()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ClassWithDataMemberIsRequiredFalse),
            nameof(ClassWithDataMemberIsRequiredFalse.StringProperty));

        // Assert
        Assert.False(metadata.IsBindingRequired);
    }

    [Fact]
    public void IsBindingRequiredDataMemberIsRequiredTrueWithoutDataContract_False()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract),
            nameof(ClassWithDataMemberIsRequiredTrueWithoutDataContract.StringProperty));

        // Assert
        Assert.False(metadata.IsBindingRequired);
    }

    [Fact]
    public void PropertySetter_NotNull_ForPublicSetProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ClassWithPublicSetProperty),
            nameof(ClassWithPublicSetProperty.StringProperty));

        // Assert
        Assert.NotNull(metadata.PropertySetter);
        Assert.NotNull(metadata.PropertyGetter);
    }

    [Fact]
    public void PropertySetter_Null_ForPrivateSetProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ClassWithPrivateSetProperty),
            nameof(ClassWithPrivateSetProperty.StringProperty));

        // Assert
        Assert.Null(metadata.PropertySetter);
        Assert.NotNull(metadata.PropertyGetter);
    }

    [Fact]
    public void Metadata_Null_ForPrivateGetProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(ClassWithPrivateGetProperty);
        var propertyName = nameof(ClassWithPrivateGetProperty.StringProperty);

        // Act & Assert
        var metadata = metadataProvider.GetMetadataForType(type);
        Assert.Null(metadata.Properties[propertyName]);
    }

    [Fact]
    public void BindingBehavior_GetMetadataForType_UsesDefaultValues()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(BindingBehaviorModel);

        // Act
        var metadata = metadataProvider.GetMetadataForType(type);

        // Assert
        Assert.True(metadata.IsBindingAllowed);
        Assert.False(metadata.IsBindingRequired);
    }

    [Fact]
    public void BindingBehavior_Override_RequiredOnProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(BindingBehaviorModel);
        var propertyName = nameof(BindingBehaviorModel.BindRequiredOverride);

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(type, propertyName);

        // Assert
        Assert.True(metadata.IsBindingAllowed);
        Assert.True(metadata.IsBindingRequired);
    }

    [Fact]
    public void BindingBehavior_Override_OptionalOnProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(BindingBehaviorModel);
        var propertyName = nameof(BindingBehaviorModel.BindingBehaviorOptionalOverride);

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(type, propertyName);

        // Assert
        Assert.True(metadata.IsBindingAllowed);
        Assert.False(metadata.IsBindingRequired);
    }

    [Fact]
    public void BindingBehavior_Never_DuplicatedPropertyAndClass()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(BindingBehaviorModel);
        var propertyName = nameof(BindingBehaviorModel.BindingBehaviorNeverDuplicatedFromClass);

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(type, propertyName);

        // Assert
        Assert.False(metadata.IsBindingAllowed);
        Assert.False(metadata.IsBindingRequired);
    }

    [Fact]
    public void BindingBehavior_Never_SetOnClass()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var type = typeof(BindingBehaviorModel);
        var propertyName = nameof(BindingBehaviorModel.BindingBehaviorNeverSetOnClass);

        // Act
        var metadata = metadataProvider.GetMetadataForProperty(type, propertyName);

        // Assert
        Assert.False(metadata.IsBindingAllowed);
        Assert.False(metadata.IsBindingRequired);
    }

    private IModelMetadataProvider CreateProvider(params object[] attributes)
    {
        return new AttributeInjectModelMetadataProvider(attributes);
    }

    private class ClassWithPrivateSetProperty
    {
        public string StringProperty { private set; get; }
    }

    private class ClassWithPublicSetProperty
    {
        public string StringProperty { set; get; }
    }

    private class ClassWithPrivateGetProperty
    {
        public string StringProperty { set; private get; }
    }

    [DataContract]
    private class ClassWithDataMemberIsRequiredTrue
    {
        [DataMember(IsRequired = true)]
        public string StringProperty { get; set; }
    }

    [DataContract]
    private class ClassWithDataMemberIsRequiredFalse
    {
        [DataMember(IsRequired = false)]
        public string StringProperty { get; set; }

        [DataMember(IsRequired = false)]
        public int IntProperty { get; set; }
    }

    private class ClassWithDataMemberIsRequiredTrueWithoutDataContract
    {
        [DataMember(IsRequired = true)]
        public string StringProperty { get; set; }
    }

    private class TestBinderTypeProvider : IBinderTypeProviderMetadata
    {
        public Type BinderType { get; set; }

        public BindingSource BindingSource { get; set; }
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

    public class CoolUser
    {
        public string Name { get; set; }
    }

    public class TypeBasedBinderAttribute : Attribute, IModelNameProvider
    {
        public string Name { get; set; }
    }

    public class NonTypeBasedBinderAttribute : Attribute, IModelNameProvider
    {
        public string Name { get; set; }
    }

    [TypeBasedBinder(Name = "PersonType")]
    public class Person
    {
        public Person Parent { get; set; }

        [NonTypeBasedBinder(Name = "GrandParentProperty")]
        public Person GrandParent { get; set; }

        public void Update(Person person)
        {
        }

        public void Save([NonTypeBasedBinder(Name = "PersonParameter")] Person person)
        {
        }
    }

    private class ScaffoldColumnModel
    {
        public int NoAttribute { get; set; }

        [ScaffoldColumn(scaffold: true)]
        public int ScaffoldColumnTrue { get; set; }

        [ScaffoldColumn(scaffold: false)]
        public int ScaffoldColumnFalse { get; set; }
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

    [Bind(new[] { nameof(IsAdmin), nameof(UserName) }, Prefix = "TypePrefix")]
    private class User
    {
        public int Id { get; set; }

        public bool IsAdmin { get; set; }

        public int UserName { get; set; }

        public int NotIncludedOrExcluded { get; set; }
    }

    private class RequiredMember
    {
        [Required]
        public string RequiredProperty { get; set; }
    }

    [BindNever]
    private class BindingBehaviorModel
    {
        [BindRequired]
        public int BindRequiredOverride { get; set; }

        [BindingBehavior(BindingBehavior.Optional)]
        public int BindingBehaviorOptionalOverride { get; set; }

        [BindingBehavior(BindingBehavior.Never)]
        public int BindingBehaviorNeverDuplicatedFromClass { get; set; }

        public int BindingBehaviorNeverSetOnClass { get; set; }
    }

    private class AttributeInjectModelMetadataProvider : DefaultModelMetadataProvider
    {
        private readonly object[] _attributes;

        public AttributeInjectModelMetadataProvider(object[] attributes)
            : base(
                  new DefaultCompositeMetadataDetailsProvider(new IMetadataDetailsProvider[]
                  {
                          new DefaultBindingMetadataProvider(),
                          new DataAnnotationsMetadataProvider(
                              new MvcOptions(),
                              Options.Create(new MvcDataAnnotationsLocalizationOptions()),
                              stringLocalizerFactory: null),
                  }),
                  Options.Create(new MvcOptions()))
        {
            _attributes = attributes;
        }

        protected override DefaultMetadataDetails CreateTypeDetails(ModelMetadataIdentity key)
        {
            var entry = base.CreateTypeDetails(key);
            if (_attributes?.Length > 0)
            {
                return new DefaultMetadataDetails(
                    key,
                    new ModelAttributes(
                        _attributes.Concat(entry.ModelAttributes.TypeAttributes).ToArray(),
                        Array.Empty<object>(),
                        Array.Empty<object>()));
            }

            return entry;
        }

        protected override DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key)
        {
            var entries = base.CreatePropertyDetails(key);
            return entries.Select(e =>
            {
                return new DefaultMetadataDetails(
                    e.Key,
                    new ModelAttributes(
                        e.ModelAttributes.TypeAttributes,
                        _attributes.Concat(e.ModelAttributes.PropertyAttributes),
                        Array.Empty<object>()));
            })
            .ToArray();
        }
    }
}
