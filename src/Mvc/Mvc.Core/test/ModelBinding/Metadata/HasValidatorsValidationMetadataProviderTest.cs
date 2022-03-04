// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class HasValidatorsValidationMetadataProviderTest
    {
        [Fact]
        public void CreateValidationMetadata_DoesNotSetHasValidators_IfNonMetadataBasedProviderExists()
        {
            // Arrange
            var validationProviders = new IModelValidatorProvider[]
            {
                new DefaultModelValidatorProvider(),
                Mock.Of<IModelValidatorProvider>(),
            };
            var metadataProvider = new HasValidatorsValidationMetadataProvider(validationProviders);

            var key = ModelMetadataIdentity.ForType(typeof(object));
            var modelAttributes = new ModelAttributes(new object[0], new object[0], new object[0]);
            var context = new ValidationMetadataProviderContext(key, modelAttributes);

            // Act
            metadataProvider.CreateValidationMetadata(context);

            // Assert
            Assert.Null(context.ValidationMetadata.HasValidators);
        }

        [Fact]
        public void CreateValidationMetadata_DoesNotSetHasValidators_IfProviderIsConfigured()
        {
            // Arrange
            var validationProviders = new IModelValidatorProvider[0];
            var metadataProvider = new HasValidatorsValidationMetadataProvider(validationProviders);

            var key = ModelMetadataIdentity.ForType(typeof(object));
            var modelAttributes = new ModelAttributes(new object[0], new object[0], new object[0]);
            var context = new ValidationMetadataProviderContext(key, modelAttributes);

            // Act
            metadataProvider.CreateValidationMetadata(context);

            // Assert
            Assert.Null(context.ValidationMetadata.HasValidators);
        }

        [Fact]
        public void CreateValidationMetadata_SetsHasValidatorsToTrue_IfProviderReturnsTrue()
        {
            // Arrange
            var metadataBasedModelValidatorProvider = new Mock<IMetadataBasedModelValidatorProvider>();
            metadataBasedModelValidatorProvider.Setup(p => p.HasValidators(typeof(object), It.IsAny<IList<object>>()))
                .Returns(true)
                .Verifiable();

            var validationProviders = new IModelValidatorProvider[]
            {
                 new DefaultModelValidatorProvider(),
                 metadataBasedModelValidatorProvider.Object,

            };
            var metadataProvider = new HasValidatorsValidationMetadataProvider(validationProviders);

            var key = ModelMetadataIdentity.ForType(typeof(object));
            var modelAttributes = new ModelAttributes(new object[0], new object[0], new object[0]);
            var context = new ValidationMetadataProviderContext(key, modelAttributes);

            // Act
            metadataProvider.CreateValidationMetadata(context);

            // Assert
            Assert.True(context.ValidationMetadata.HasValidators);
            metadataBasedModelValidatorProvider.Verify();
        }

        [Fact]
        public void CreateValidationMetadata_SetsHasValidatorsToFalse_IfNoProviderReturnsTrue()
        {
            // Arrange
            var provider = Mock.Of<IMetadataBasedModelValidatorProvider>(p => p.HasValidators(typeof(object), It.IsAny<IList<object>>()) == false);
            var validationProviders = new IModelValidatorProvider[]
            {
                 new DefaultModelValidatorProvider(),
                 provider,
            };
            var metadataProvider = new HasValidatorsValidationMetadataProvider(validationProviders);

            var key = ModelMetadataIdentity.ForType(typeof(object));
            var modelAttributes = new ModelAttributes(new object[0], new object[0], new object[0]);
            var context = new ValidationMetadataProviderContext(key, modelAttributes);

            // Act
            metadataProvider.CreateValidationMetadata(context);

            // Assert
            Assert.False(context.ValidationMetadata.HasValidators);
        }

        [Fact]
        public void CreateValidationMetadata_DoesNotOverrideExistingHasValidatorsValue()
        {
            // Arrange
            var provider = Mock.Of<IMetadataBasedModelValidatorProvider>(p => p.HasValidators(typeof(object), It.IsAny<IList<object>>()) == false);
            var validationProviders = new IModelValidatorProvider[]
            {
                 new DefaultModelValidatorProvider(),
                 provider,
            };
            var metadataProvider = new HasValidatorsValidationMetadataProvider(validationProviders);

            var key = ModelMetadataIdentity.ForType(typeof(object));
            var modelAttributes = new ModelAttributes(new object[0], new object[0], new object[0]);
            var context = new ValidationMetadataProviderContext(key, modelAttributes);
            
            // Initialize this value.
            context.ValidationMetadata.HasValidators = true;

            // Act
            metadataProvider.CreateValidationMetadata(context);

            // Assert
            Assert.True(context.ValidationMetadata.HasValidators);
        }
    }
}
