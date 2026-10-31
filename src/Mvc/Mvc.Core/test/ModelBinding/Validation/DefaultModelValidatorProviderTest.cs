// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

// Integration tests for the default configuration of ModelMetadata and Validation providers
public class DefaultModelValidatorProviderTest
{
    [Fact]
    public void CreateValidators_ForIValidatableObject()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForType(typeof(ValidatableObject));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        var validatorItem = Assert.Single(validatorItems);
        Assert.IsType<ValidatableObjectAdapter>(validatorItem.Validator);
    }

    [Fact]
    public void CreateValidators_ModelValidatorAttributeOnClass()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForType(typeof(ModelValidatorAttributeOnClass));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        var validator = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validatorItems).Validator);
        Assert.Equal("Class", validator.Tag);
    }

    [Fact]
    public void CreateValidators_ModelValidatorAttributeOnProperty()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelValidatorAttributeOnProperty),
            nameof(ModelValidatorAttributeOnProperty.Property));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        var validator = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validatorItems).Validator);
        Assert.Equal("Property", validator.Tag);
    }

    [Fact]
    public void CreateValidators_ModelValidatorAttributeOnPropertyAndClass()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelValidatorAttributeOnPropertyAndClass),
            nameof(ModelValidatorAttributeOnPropertyAndClass.Property));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.Equal(2, validatorItems.Count);
        Assert.Single(validatorItems, v => Assert.IsType<CustomModelValidatorAttribute>(v.Validator).Tag == "Class");
        Assert.Single(validatorItems, v => Assert.IsType<CustomModelValidatorAttribute>(v.Validator).Tag == "Property");
    }

    [Fact]
    public void CreateValidators_FromModelMetadataType_SingleValidator()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ProductViewModel),
            nameof(ProductViewModel.Id));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        var adapter = Assert.IsType<DataAnnotationsModelValidator>(Assert.Single(validatorItems).Validator);
        Assert.IsType<RangeAttribute>(adapter.Attribute);
    }

    [Fact]
    public void CreateValidators_FromModelMetadataType_MergedValidators()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ProductViewModel),
            nameof(ProductViewModel.Name));
        var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.Equal(2, validatorItems.Count);
        Assert.Single(validatorItems, v => ((DataAnnotationsModelValidator)v.Validator).Attribute is RegularExpressionAttribute);
        Assert.Single(validatorItems, v => ((DataAnnotationsModelValidator)v.Validator).Attribute is StringLengthAttribute);
    }

    [Fact]
    public void HasValidators_ReturnsTrue_IfMetadataIsIModelValidator()
    {
        // Arrange
        var validatorProvider = new DefaultModelValidatorProvider();
        var attributes = new object[] { new RequiredAttribute(), new CustomModelValidatorAttribute(), new BindRequiredAttribute(), };

        // Act
        var result = validatorProvider.HasValidators(typeof(object), attributes);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidators_ReturnsFalse_IfNoMetadataIsIModelValidator()
    {
        // Arrange
        var validatorProvider = new DefaultModelValidatorProvider();
        var attributes = new object[] { new RequiredAttribute(), new BindRequiredAttribute(), };

        // Act
        var result = validatorProvider.HasValidators(typeof(object), attributes);

        // Assert
        Assert.False(result);
    }

    private static IList<ValidatorItem> GetValidatorItems(ModelMetadata metadata)
    {
        return metadata.ValidatorMetadata.Select(v => new ValidatorItem(v)).ToList();
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
