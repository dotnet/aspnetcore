// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class DefaultModelMetadataBindingDetailsProviderTest
{
    [Fact]
    public void CreateBindingDetails_FindsBinderTypeProvider()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute() { BinderType = typeof(HeaderModelBinder) },
                new ModelBinderAttribute() { BinderType = typeof(ArrayModelBinder<string>) },
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
    }

    [Fact]
    public void CreateBindingDetails_FindsBinderTypeProvider_IfNullFallsBack()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute(),
                new ModelBinderAttribute() { BinderType = typeof(HeaderModelBinder) },
                new ModelBinderAttribute() { BinderType = typeof(ArrayModelBinder<string>) },
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
    }

    [Fact]
    public void CreateBindingDetails_FindsModelName()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute() { Name = "Product" },
                new ModelBinderAttribute() { Name = "Order" },
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal("Product", context.BindingMetadata.BinderModelName);
    }

    [Fact]
    public void CreateBindingDetails_FindsModelName_IfNullFallsBack()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute(),
                new ModelBinderAttribute() { Name = "Product" },
                new ModelBinderAttribute() { Name = "Order" },
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal("Product", context.BindingMetadata.BinderModelName);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingSource()
    {
        // Arrange
        var attributes = new object[]
        {
                new BindingSourceModelBinderAttribute(BindingSource.Body),
                new BindingSourceModelBinderAttribute(BindingSource.Query),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingSource_IfNullFallsBack()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute(),
                new BindingSourceModelBinderAttribute(BindingSource.Body),
                new BindingSourceModelBinderAttribute(BindingSource.Query),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorNever_OnProperty()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Never),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindNever_OnProperty()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindNeverAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorOptional_OnProperty()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Optional),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorRequired_OnProperty()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Required),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindRequired_OnProperty()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindRequiredAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorNever_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Never),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindNever_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new BindNeverAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorOptional_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Optional),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindingBehaviorRequired_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Required),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindRequired_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new BindRequiredAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsCustomAttributes_OnParameter()
    {
        // Arrange
        var parameterAttributes = new object[]
        {
                new CustomAttribute { Identifier = "Instance1" },
                new CustomAttribute { Identifier = "Instance2" }
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForParameter(ParameterInfos.SampleParameterInfo),
            new ModelAttributes(Array.Empty<object>(), null, parameterAttributes));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Collection(context.Attributes,
            a => Assert.Equal("Instance1", ((CustomAttribute)a).Identifier),
            a => Assert.Equal("Instance2", ((CustomAttribute)a).Identifier));
        Assert.Equal(2, context.ParameterAttributes.Count);
    }

    // These attributes have conflicting behavior - the 'required' behavior should be used because
    // of ordering.
    [Fact]
    public void CreateBindingDetails_UsesFirstAttribute()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Required),
                new BindNeverAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindRequired_OnContainerClass()
    {
        // Arrange
        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindRequiredOnClass).GetProperty(nameof(BindRequiredOnClass.Property)), typeof(int), typeof(BindRequiredOnClass)),
            new ModelAttributes(new object[0], new object[0], null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindNever_OnContainerClass()
    {
        // Arrange
        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindNeverOnClass).GetProperty(nameof(BindNeverOnClass.Property)), typeof(int), typeof(BindNeverOnClass)),
            new ModelAttributes(new object[0], new object[0], null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_FindsBindNever_OnBaseClass()
    {
        // Arrange
        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindNeverOnClass).GetProperty(nameof(BindNeverOnClass.Property)), typeof(int), typeof(BindNeverOnClass)),
            new ModelAttributes(new object[0], new object[0], null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_OverrideBehaviorOnClass_OverrideWithOptional()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Optional)
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindNeverOnClass).GetProperty(nameof(BindNeverOnClass.Property)), typeof(int), typeof(BindNeverOnClass)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_OverrideBehaviorOnClass_OverrideWithRequired()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindRequiredAttribute()
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindNeverOnClass).GetProperty(nameof(BindNeverOnClass.Property)), typeof(int), typeof(BindNeverOnClass)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_OverrideInheritedBehaviorOnClass_OverrideWithRequired()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindRequiredAttribute()
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(InheritedBindNeverOnClass).GetProperty(nameof(InheritedBindNeverOnClass.Property)), typeof(int), typeof(InheritedBindNeverOnClass)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Fact]
    public void CreateBindingDetails_OverrideBehaviorOnClass_OverrideWithNever()
    {
        // Arrange
        var propertyAttributes = new object[]
        {
                new BindNeverAttribute(),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindRequiredOnClass).GetProperty(nameof(BindRequiredOnClass.Property)), typeof(int), typeof(BindRequiredOnClass)),
            new ModelAttributes(new object[0], propertyAttributes, null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
        Assert.False(context.BindingMetadata.IsBindingRequired);
    }

    // This overrides an inherited class-level attribute with a different class-level attribute.
    [Fact]
    public void CreateBindingDetails_OverrideBehaviorOnBaseClass_OverrideWithRequired_OnClass()
    {
        // Arrange
        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(BindRequiredOverridesInheritedBindNever).GetProperty(nameof(BindRequiredOverridesInheritedBindNever.Property)), typeof(int), typeof(BindRequiredOverridesInheritedBindNever)),
            new ModelAttributes(new object[0], new object[0], null));

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingAllowed);
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateBindingDetails_BindingBehaviorLeftAlone_ForTypeMetadata(bool initialValue)
    {
        // Arrange
        var attributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Required),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)),
            new ModelAttributes(attributes, null, null));

        // These values shouldn't be changed since this is a Type-Metadata
        context.BindingMetadata.IsBindingAllowed = initialValue;
        context.BindingMetadata.IsBindingRequired = initialValue;

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    // Unlike most model metadata settings, BindingBehavior can be specified on the *container type*
    // but not on the property type.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateBindingDetails_BindingBehaviorLeftAlone_ForAttributeOnPropertyType(bool initialValue)
    {
        // Arrange
        var typeAttributes = new object[]
        {
                new BindingBehaviorAttribute(BindingBehavior.Required),
        };

        var context = new BindingMetadataProviderContext(
            ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string)),
            new ModelAttributes(typeAttributes, new object[0], null));

        // These values shouldn't be changed since this is a Type-Metadata
        context.BindingMetadata.IsBindingAllowed = initialValue;
        context.BindingMetadata.IsBindingRequired = initialValue;

        var provider = CreateBindingMetadataProvider();

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    private class DefaultConstructorType { }

    [Fact]
    public void GetBoundConstructor_DefaultConstructor_ReturnsNull()
    {
        // Arrange
        var type = typeof(DefaultConstructorType);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    private class ParameterlessConstructorType
    {
        public ParameterlessConstructorType() { }
    }

    [Fact]
    public void GetBoundConstructor_ParameterlessConstructor_ReturnsNull()
    {
        // Arrange
        var type = typeof(ParameterlessConstructorType);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    private class NonPublicParameterlessConstructorType
    {
        protected NonPublicParameterlessConstructorType() { }
    }

    [Fact]
    public void GetBoundConstructor_DoesNotReturnsNonPublicParameterlessConstructor()
    {
        // Arrange
        var type = typeof(NonPublicParameterlessConstructorType);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    private class MultipleConstructorType
    {
        public MultipleConstructorType() { }
        public MultipleConstructorType(string prop) { }
    }

    [Fact]
    public void GetBoundConstructor_ReturnsParameterlessConstructor_ForTypeWithMultipleConstructors()
    {
        // Arrange
        var type = typeof(NonPublicParameterlessConstructorType);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    private record RecordTypeWithPrimaryConstructor(string name)
    {
    }

    [Fact]
    public void GetBoundConstructor_ReturnsPrimaryConstructor_ForRecordType()
    {
        // Arrange
        var type = typeof(RecordTypeWithPrimaryConstructor);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(
            result.GetParameters(),
            p => Assert.Equal("name", p.Name));
    }

    private record RecordTypeWithDefaultConstructor
    {
        public string Name { get; init; }

        public int Age { get; init; }
    }

    private record RecordTypeWithParameterlessConstructor
    {
        public RecordTypeWithParameterlessConstructor() { }

        public string Name { get; init; }

        public int Age { get; init; }
    }

    [Theory]
    [InlineData(typeof(RecordTypeWithDefaultConstructor))]
    [InlineData(typeof(RecordTypeWithParameterlessConstructor))]
    public void GetBoundConstructor_ReturnsNull_ForRecordTypeWithParameterlessConstructor(Type type)
    {
        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    private record RecordTypeWithMultipleConstructors(string Name)
    {
        public RecordTypeWithMultipleConstructors(string Name, int age) : this(Name) => Age = age;

        public RecordTypeWithMultipleConstructors(int age) : this(string.Empty, age) { }

        public int Age { get; set; }
    }

    [Fact]
    public void GetBoundConstructor_ReturnsNull_ForRecordTypeWithMultipleConstructors()
    {
        // Arrange
        var type = typeof(RecordTypeWithMultipleConstructors);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBoundConstructor_ReturnsPrimaryConstructor_ForRecordTypeInherited()
    {
        // Arrange
        var type = typeof(Model);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(
            result.GetParameters(),
            p => Assert.Equal("Name", p.Name),
            p => Assert.Equal("Value", p.Name));
    }

    public record BaseModel(int Value);
    public record Model(string Name, int Value) : BaseModel(Value);

    private record RecordTypeWithConformingSynthesizedConstructor
    {
        public RecordTypeWithConformingSynthesizedConstructor(string Name, int Age)
        {
        }

        public string Name { get; set; }

        public int Age { get; set; }
    }

    [Fact]
    public void GetBoundConstructor_ReturnsConformingSynthesizedConstructor()
    {
        // Arrange
        var type = typeof(RecordTypeWithConformingSynthesizedConstructor);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(
            result.GetParameters(),
            p => Assert.Equal("Name", p.Name),
            p => Assert.Equal("Age", p.Name));
    }

    private record RecordTypeWithNonConformingSynthesizedConstructor
    {
        public RecordTypeWithNonConformingSynthesizedConstructor(string name, string age)
        {
        }

        public string Name { get; set; }

        public int Age { get; set; }
    }

    [Fact]
    public void GetBoundConstructor_ReturnsNull_IfSynthesizedConstructorIsNonConforming()
    {
        // Arrange
        var type = typeof(RecordTypeWithNonConformingSynthesizedConstructor);

        // Act
        var result = DefaultBindingMetadataProvider.GetBoundConstructor(type);

        // Assert
        Assert.Null(result);
    }

    [BindNever]
    private class BindNeverOnClass
    {
        public string Property { get; set; }
    }

    private class InheritedBindNeverOnClass : BindNeverOnClass
    {
    }

    [BindRequired]
    private class BindRequiredOnClass
    {
        public string Property { get; set; }
    }

    [BindRequired]
    private class BindRequiredOverridesInheritedBindNever : BindNeverOnClass
    {
    }

    private class BindingSourceModelBinderAttribute : ModelBinderAttribute
    {
        public BindingSourceModelBinderAttribute(BindingSource bindingSource)
        {
            BindingSource = bindingSource;
        }
    }

    private class ParameterInfos
    {
        public void Method(object param1)
        {
        }

        public static ParameterInfo SampleParameterInfo
            = typeof(ParameterInfos)
                .GetMethod(nameof(ParameterInfos.Method))
                .GetParameters()[0];
    }

    private class CustomAttribute : Attribute
    {
        public string Identifier { get; set; }
    }

    private DefaultBindingMetadataProvider CreateBindingMetadataProvider()
        => new DefaultBindingMetadataProvider();
}
