// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DefaultValidationMetadataTest
    {
        [Fact]
        public void GetValidationDetails_MarkedWithClientValidator_ReturnsValidator()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attribute = new TestClientModelValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));

            // Act
            provider.GetValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        [Fact]
        public void GetValidationDetails_MarkedWithModelValidator_ReturnsValidator()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attribute = new TestModelValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));

            // Act
            provider.GetValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        [Fact]
        public void GetValidationDetails_Validator_AlreadyInContext_Ignores()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attribute = new TestValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(int), "Length", typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0]));
            context.ValidationMetadata.ValidatorMetadata.Add(attribute);

            // Act
            provider.GetValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        private class TestModelValidationAttribute : Attribute, IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class TestClientModelValidationAttribute : Attribute, IClientModelValidator
        {
            public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class TestValidationAttribute : Attribute, IModelValidator, IClientModelValidator
        {
            public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}