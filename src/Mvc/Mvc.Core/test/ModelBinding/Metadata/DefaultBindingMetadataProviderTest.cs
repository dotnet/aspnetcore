// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

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

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
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
    }
}