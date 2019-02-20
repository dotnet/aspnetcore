// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class BindingInfoTest
    {
        [Fact]
        public void GetBindingInfo_WithAttributes_ConstructsBindingInfo()
        {
            // Arrange
            var attributes = new object[]
            {
                new FromQueryAttribute { Name = "Test" },
            };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same("Test", bindingInfo.BinderModelName);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        }

        [Fact]
        public void GetBindingInfo_ReadsPropertyPredicateProvider()
        {
            // Arrange
            var bindAttribute = new BindAttribute(include: "SomeProperty");
            var attributes = new object[]
            {
                bindAttribute,
            };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(bindAttribute, bindingInfo.PropertyFilterProvider);
        }

        [Fact]
        public void GetBindingInfo_ReadsRequestPredicateProvider()
        {
            // Arrange
            var attributes = new object[]
            {
                new BindPropertyAttribute { Name = "PropertyPrefix", SupportsGet = true, },
            };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same("PropertyPrefix", bindingInfo.BinderModelName);
            Assert.NotNull(bindingInfo.RequestPredicate);
        }

        [Fact]
        public void GetBindingInfo_ReturnsNull_IfNoBindingAttributesArePresent()
        {
            // Arrange
            var attributes = new object[] { new ControllerAttribute(), new BindNeverAttribute(), };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.Null(bindingInfo);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesValuesFromBindingInfo_IfAttributesPresent()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute { BinderType = typeof(ComplexTypeModelBinder), Name = "Test" },
            };
            var modelType = typeof(Guid);
            var provider = new TestModelMetadataProvider();
            provider.ForType(modelType).BindingDetails(metadata =>
            {
                metadata.BindingSource = BindingSource.Special;
                metadata.BinderType = typeof(SimpleTypeModelBinder);
                metadata.BinderModelName = "Different";
            });
            var modelMetadata = provider.GetMetadataForType(modelType);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(typeof(ComplexTypeModelBinder), bindingInfo.BinderType);
            Assert.Same("Test", bindingInfo.BinderModelName);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesBinderNameFromModelMetadata_WhenNotFoundViaAttributes()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute(typeof(ComplexTypeModelBinder)),
                new ControllerAttribute(),
                new BindNeverAttribute(),
            };
            var modelType = typeof(Guid);
            var provider = new TestModelMetadataProvider();
            provider.ForType(modelType).BindingDetails(metadata =>
            {
                metadata.BindingSource = BindingSource.Special;
                metadata.BinderType = typeof(SimpleTypeModelBinder);
                metadata.BinderModelName = "Different";
            });
            var modelMetadata = provider.GetMetadataForType(modelType);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(typeof(ComplexTypeModelBinder), bindingInfo.BinderType);
            Assert.Same("Different", bindingInfo.BinderModelName);
            Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesModelBinderFromModelMetadata_WhenNotFoundViaAttributes()
        {
            // Arrange
            var attributes = new object[] { new ControllerAttribute(), new BindNeverAttribute(), };
            var modelType = typeof(Guid);
            var provider = new TestModelMetadataProvider();
            provider.ForType(modelType).BindingDetails(metadata =>
            {
                metadata.BinderType = typeof(ComplexTypeModelBinder);
            });
            var modelMetadata = provider.GetMetadataForType(modelType);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(typeof(ComplexTypeModelBinder), bindingInfo.BinderType);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesBinderSourceFromModelMetadata_WhenNotFoundViaAttributes()
        {
            // Arrange
            var attributes = new object[]
            {
                new BindPropertyAttribute(),
                new ControllerAttribute(),
                new BindNeverAttribute(),
            };
            var modelType = typeof(Guid);
            var provider = new TestModelMetadataProvider();
            provider.ForType(modelType).BindingDetails(metadata =>
            {
                metadata.BindingSource = BindingSource.Services;
            });
            var modelMetadata = provider.GetMetadataForType(modelType);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Services, bindingInfo.BindingSource);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesPropertyPredicateProviderFromModelMetadata_WhenNotFoundViaAttributes()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute(typeof(ComplexTypeModelBinder)),
                new ControllerAttribute(),
                new BindNeverAttribute(),
            };
            var propertyFilterProvider = Mock.Of<IPropertyFilterProvider>();
            var modelType = typeof(Guid);
            var provider = new TestModelMetadataProvider();
            provider.ForType(modelType).BindingDetails(metadata =>
            {
                metadata.PropertyFilterProvider = propertyFilterProvider;
            });
            var modelMetadata = provider.GetMetadataForType(modelType);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(propertyFilterProvider, bindingInfo.PropertyFilterProvider);
        }
    }
}
