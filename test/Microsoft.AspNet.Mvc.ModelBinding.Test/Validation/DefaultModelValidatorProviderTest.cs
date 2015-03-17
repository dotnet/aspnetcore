// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    // Integration tests for the default configuration of ModelMetadata and Validation providers
    public class DefaultModelValidatorProviderTest
    {
        [Fact]
        public void GetValidators_ForIValidatableObject()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForType(typeof(ValidatableObject));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            var validator = Assert.Single(validators);
            Assert.IsType<ValidatableObjectAdapter>(validator);
        }

        [Fact]
        public void GetValidators_ModelValidatorAttributeOnClass()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForType(typeof(ModelValidatorAttributeOnClass));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            var validator = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validators));
            Assert.Equal("Class", validator.Tag);
        }

        [Fact]
        public void GetValidators_ModelValidatorAttributeOnProperty()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelValidatorAttributeOnProperty), 
                nameof(ModelValidatorAttributeOnProperty.Property));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            var validator = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validators));
            Assert.Equal("Property", validator.Tag);
        }

        [Fact]
        public void GetValidators_ModelValidatorAttributeOnPropertyAndClass()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelValidatorAttributeOnPropertyAndClass),
                nameof(ModelValidatorAttributeOnPropertyAndClass.Property));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.Equal(2, validators.Count);
            Assert.Single(validators, v => Assert.IsType<CustomModelValidatorAttribute>(v).Tag == "Class");
            Assert.Single(validators, v => Assert.IsType<CustomModelValidatorAttribute>(v).Tag == "Property");
        }

        // RangeAttribute is an example of a ValidationAttribute with it's own adapter type.
        [Fact]
        public void GetValidators_DataAnnotationsAttribute_SpecificAdapter()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(RangeAttributeOnProperty),
                nameof(RangeAttributeOnProperty.Property));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.IsType<RangeAttributeAdapter>(Assert.Single(validators));
        }
        
        [Fact]
        public void GetValidators_DataAnnotationsAttribute_DefaultAdapter()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(CustomValidationAttributeOnProperty),
                nameof(CustomValidationAttributeOnProperty.Property));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

             Assert.IsType<DataAnnotationsModelValidator>(Assert.Single(validators));
        }

        [Fact]
        public void GetValidators_FromModelMetadataType_SingleValidator()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ProductViewModel),
                nameof(ProductViewModel.Id));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.IsType<RangeAttributeAdapter>(Assert.Single(validators));
        }

        [Fact]
        public void GetValidators_FromModelMetadataType_MergedValidators()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ProductViewModel),
                nameof(ProductViewModel.Name));
            var context = new ModelValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.Equal(2, validators.Count);
            Assert.Single(validators, v => v is RegularExpressionAttributeAdapter);
            Assert.Single(validators, v => v is StringLengthAttributeAdapter);
        }

        private class ValidatableObject : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return null;
            }
        }

        [CustomModelValidator(Tag = "Class")]
        private class ModelValidatorAttributeOnClass
        {
        }

        
        private class ModelValidatorAttributeOnProperty
        {
            [CustomModelValidator(Tag = "Property")]
            public string Property { get; set; }
        }

        private class ModelValidatorAttributeOnPropertyAndClass
        {
            [CustomModelValidator(Tag = "Property")]
            public ModelValidatorAttributeOnClass Property { get; set; }
        }

        private class CustomModelValidatorAttribute : Attribute, IModelValidator
        {
            public string Tag { get; set; }

            public bool IsRequired
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class RangeAttributeOnProperty
        {
            [Range(0, 10)]
            public int Property { get; set; }
        }

        private class CustomValidationAttribute : ValidationAttribute
        {
        }

        private class CustomValidationAttributeOnProperty
        {
            [CustomValidation]
            public int Property { get; set; }
        }

        private class ProductEntity
        {
            [Range(0, 10)]
            public int Id { get; set; }

            [RegularExpression(".*")]
            public string Name { get; set; }
        }

        [ModelMetadataType(typeof(ProductEntity))]
        private class ProductViewModel
        {
            public int Id { get; set; }

            [StringLength(4)]
            public string Name { get; set; }
        }
    }
}