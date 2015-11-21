// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Core;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DefaultModelMetadataBindingDetailsProviderTest
    {
        [Fact]
        public void GetBindingDetails_FindsBinderTypeProvider()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute() { BinderType = typeof(HeaderModelBinder) },
                new ModelBinderAttribute() { BinderType = typeof(ArrayModelBinder<string>) },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
        }

        [Fact]
        public void GetBindingDetails_FindsBinderTypeProvider_IfNullFallsBack()
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
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
        }

        [Fact]
        public void GetBindingDetails_FindsModelName()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute() { Name = "Product" },
                new ModelBinderAttribute() { Name = "Order" },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal("Product", context.BindingMetadata.BinderModelName);
        }

        [Fact]
        public void GetBindingDetails_FindsModelName_IfNullFallsBack()
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
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal("Product", context.BindingMetadata.BinderModelName);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingSource()
        {
            // Arrange
            var attributes = new object[]
            {
                new BindingSourceModelBinderAttribute(BindingSource.Body),
                new BindingSourceModelBinderAttribute(BindingSource.Query),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingSource_IfNullFallsBack()
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
                new ModelAttributes(attributes));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingBehaviorNever_OnProperty()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Never),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindNever_OnProperty()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindNeverAttribute(),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingBehaviorOptional_OnProperty()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Optional),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingBehaviorRequired_OnProperty()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Required),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindRequired_OnProperty()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindRequiredAttribute(),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        // These attributes have conflicting behavior - the 'required' behavior should be used because
        // of ordering.
        [Fact]
        public void GetBindingDetails_UsesFirstAttribute()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Required),
                new BindNeverAttribute(),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindRequired_OnContainerClass()
        {
            // Arrange
            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindRequiredOnClass)),
                new ModelAttributes(propertyAttributes: new object[0], typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindNever_OnContainerClass()
        {
            // Arrange
            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindNeverOnClass)),
                new ModelAttributes(propertyAttributes: new object[0], typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_FindsBindNever_OnBaseClass()
        {
            // Arrange
            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(InheritedBindNeverOnClass)),
                new ModelAttributes(propertyAttributes: new object[0], typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_OverrideBehaviorOnClass_OverrideWithOptional()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Optional)
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindNeverOnClass)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_OverrideBehaviorOnClass_OverrideWithRequired()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindRequiredAttribute()
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindNeverOnClass)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_OverrideInheritedBehaviorOnClass_OverrideWithRequired()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindRequiredAttribute()
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(InheritedBindNeverOnClass)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Fact]
        public void GetBindingDetails_OverrideBehaviorOnClass_OverrideWithNever()
        {
            // Arrange
            var propertyAttributes = new object[]
            {
                new BindNeverAttribute(),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindRequiredOnClass)),
                new ModelAttributes(propertyAttributes, typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
            Assert.False(context.BindingMetadata.IsBindingRequired);
        }

        // This overrides an inherited class-level attribute with a different class-level attribute.
        [Fact]
        public void GetBindingDetails_OverrideBehaviorOnBaseClass_OverrideWithRequired_OnClass()
        {
            // Arrange
            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(string), "Property", typeof(BindRequiredOverridesInheritedBindNever)),
                new ModelAttributes(propertyAttributes: new object[0], typeAttributes: new object[0]));

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.True(context.BindingMetadata.IsBindingAllowed);
            Assert.True(context.BindingMetadata.IsBindingRequired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetBindingDetails_BindingBehaviorLeftAlone_ForTypeMetadata(bool initialValue)
        {
            // Arrange
            var attributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Required),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                new ModelAttributes(attributes));

            // These values shouldn't be changed since this is a Type-Metadata
            context.BindingMetadata.IsBindingAllowed = initialValue;
            context.BindingMetadata.IsBindingRequired = initialValue;

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
        }

        // Unlike most model metadata settings, BindingBehavior can be specified on the *container type*
        // but not on the property type.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetBindingDetails_BindingBehaviorLeftAlone_ForAttributeOnPropertyType(bool initialValue)
        {
            // Arrange
            var typeAttributes = new object[]
            {
                new BindingBehaviorAttribute(BindingBehavior.Required),
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string)),
                new ModelAttributes(propertyAttributes: new object[0], typeAttributes: typeAttributes));

            // These values shouldn't be changed since this is a Type-Metadata
            context.BindingMetadata.IsBindingAllowed = initialValue;
            context.BindingMetadata.IsBindingRequired = initialValue;

            var provider = new DefaultBindingMetadataProvider(CreateMessageProvider());

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
        }

        private static ModelBindingMessageProvider CreateMessageProvider()
        {
            return new ModelBindingMessageProvider
            {
                MissingBindRequiredValueAccessor = Resources.FormatModelBinding_MissingBindRequiredMember,
                MissingKeyOrValueAccessor = Resources.FormatKeyValuePair_BothKeyAndValueMustBePresent,
                ValueMustNotBeNullAccessor = Resources.FormatModelBinding_NullValueNotValid,
            };
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
    }
}