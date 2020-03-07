// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class DefaultValidationMetadataProviderTest
    {
        [Fact]
        public void PropertyValidationFilter_ShouldValidateEntry_False_IfPropertyHasValidateNever()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attributes = new Attribute[] { new ValidateNeverAttribute() };
            var key = ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.NotNull(context.ValidationMetadata.PropertyValidationFilter);
            Assert.False(context.ValidationMetadata.PropertyValidationFilter.ShouldValidateEntry(
                new ValidationEntry(),
                new ValidationEntry()));
        }

        [Fact]
        public void PropertyValidationFilter_Null_IfPropertyHasValidateNeverOnItsType()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attributes = new Attribute[] { new ValidateNeverAttribute() };
            var key = ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, new object[0], null));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.Null(context.ValidationMetadata.PropertyValidationFilter);
        }

        [Fact]
        public void PropertyValidationFilter_Null_ForType()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attributes = new Attribute[] { new ValidateNeverAttribute() };
            var key = ModelMetadataIdentity.ForType(typeof(ValidateNeverClass));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(attributes, null, null));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.Null(context.ValidationMetadata.PropertyValidationFilter);
        }

        [Fact]
        public void PropertyValidationFilter_ShouldValidateEntry_False_IfContainingTypeHasValidateNever()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(ValidateNeverClass).GetProperty(nameof(ValidateNeverClass.ClassName)),
                typeof(string),
                typeof(ValidateNeverClass));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.NotNull(context.ValidationMetadata.PropertyValidationFilter);
            Assert.False(context.ValidationMetadata.PropertyValidationFilter.ShouldValidateEntry(
                new ValidationEntry(),
                new ValidationEntry()));
        }

        [Fact]
        public void PropertyValidationFilter_ShouldValidateEntry_False_IfContainingTypeInheritsValidateNever()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(ValidateNeverSubclass).GetProperty(nameof(ValidateNeverSubclass.SubclassName)),
                typeof(string),
                typeof(ValidateNeverSubclass));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            Assert.NotNull(context.ValidationMetadata.PropertyValidationFilter);
            Assert.False(context.ValidationMetadata.PropertyValidationFilter.ShouldValidateEntry(
                new ValidationEntry(),
                new ValidationEntry()));
        }

        [Fact]
        public void GetValidationDetails_MarkedWithClientValidator_ReturnsValidator()
        {
            // Arrange
            var provider = new DefaultValidationMetadataProvider();

            var attribute = new TestClientModelValidationAttribute();
            var attributes = new Attribute[] { attribute };
            var key = ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));

            // Act
            provider.CreateValidationMetadata(context);

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
            var key = ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));

            // Act
            provider.CreateValidationMetadata(context);

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
            var key = ModelMetadataIdentity.ForProperty(typeof(string).GetProperty(nameof(string.Length)), typeof(int), typeof(string));
            var context = new ValidationMetadataProviderContext(key, new ModelAttributes(new object[0], attributes, null));
            context.ValidationMetadata.ValidatorMetadata.Add(attribute);

            // Act
            provider.CreateValidationMetadata(context);

            // Assert
            var validatorMetadata = Assert.Single(context.ValidationMetadata.ValidatorMetadata);
            Assert.Same(attribute, validatorMetadata);
        }

        [ValidateNever]
        private class ValidateNeverClass
        {
            public string ClassName { get; set; }
        }

        private class ValidateNeverSubclass : ValidateNeverClass
        {
            public string SubclassName { get; set; }
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
            public void AddValidation(ClientModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class TestValidationAttribute : Attribute, IModelValidator, IClientModelValidator
        {
            public void AddValidation(ClientModelValidationContext context)
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