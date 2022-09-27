// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

// Integration tests for the default configuration of ModelMetadata and Validation providers
public class DefaultModelClientValidatorProviderTest
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
    public void CreateValidators_ClientModelValidatorAttributeOnClass()
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

        var validatorItem = Assert.Single(validatorItems);
        var customModelValidator = Assert.IsType<CustomModelValidatorAttribute>(validatorItem.Validator);
        Assert.Equal("Class", customModelValidator.Tag);
    }

    [Fact]
    public void CreateValidators_ClientModelValidatorAttributeOnProperty()
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

        var validatorItem = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validatorItems).Validator);
        Assert.Equal("Property", validatorItem.Tag);
    }

    [Fact]
    public void CreateValidators_ClientModelValidatorAttributeOnPropertyAndClass()
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

    // RangeAttribute is an example of a ValidationAttribute with it's own adapter type.
    [Fact]
    public void CreateValidators_ClientValidatorAttribute_SpecificAdapter()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(RangeAttributeOnProperty),
            nameof(RangeAttributeOnProperty.Property));
        var context = new ClientValidatorProviderContext(metadata, GetClientValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.Equal(2, validatorItems.Count);
        Assert.Single(validatorItems, v => v.Validator is RangeAttributeAdapter);
        Assert.Single(validatorItems, v => v.Validator is RequiredAttributeAdapter);
    }

    [Fact]
    public void CreateValidators_ClientValidatorAttribute_DefaultAdapter()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(CustomValidationAttributeOnProperty),
            nameof(CustomValidationAttributeOnProperty.Property));
        var context = new ClientValidatorProviderContext(metadata, GetClientValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.IsType<CustomValidationAttribute>(Assert.Single(validatorItems).Validator);
    }

    [Fact]
    public void CreateValidators_FromModelMetadataType_SingleValidator()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ProductViewModel),
            nameof(ProductViewModel.Id));
        var context = new ClientValidatorProviderContext(metadata, GetClientValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.Equal(2, validatorItems.Count);
        Assert.Single(validatorItems, v => v.Validator is RangeAttributeAdapter);
        Assert.Single(validatorItems, v => v.Validator is RequiredAttributeAdapter);
    }

    [Fact]
    public void CreateValidators_FromModelMetadataType_MergedValidators()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ProductViewModel),
            nameof(ProductViewModel.Name));
        var context = new ClientValidatorProviderContext(metadata, GetClientValidatorItems(metadata));

        // Act
        validatorProvider.CreateValidators(context);

        // Assert
        var validatorItems = context.Results;

        Assert.Equal(2, validatorItems.Count);
        Assert.Single(validatorItems, v => v.Validator is RegularExpressionAttributeAdapter);
        Assert.Single(validatorItems, v => v.Validator is StringLengthAttributeAdapter);
    }

    private IList<ClientValidatorItem> GetClientValidatorItems(ModelMetadata metadata)
    {
        return metadata.ValidatorMetadata.Select(v => new ClientValidatorItem(v)).ToList();
    }

    private IList<ValidatorItem> GetValidatorItems(ModelMetadata metadata)
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

    private class CustomValidationAttribute : Attribute, IClientModelValidator
    {
        public void AddValidation(ClientModelValidationContext context)
        {
        }
    }

    private class CustomValidationAttributeOnProperty
    {
        [CustomValidation]
        public string Property { get; set; }
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
