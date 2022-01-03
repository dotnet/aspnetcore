// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class DataAnnotationsClientModelValidatorProviderTest
{
    private readonly IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

    [Fact]
    public void CreateValidators_AddsRequiredAttribute_ForIsRequiredTrue()
    {
        // Arrange
        var provider = new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);

        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(DummyRequiredAttributeHelperClass),
            nameof(DummyRequiredAttributeHelperClass.ValueTypeWithoutAttribute));

        var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = Assert.Single(providerContext.Results);
        Assert.IsType<RequiredAttributeAdapter>(validatorItem.Validator);
    }

    [Fact]
    public void CreateValidators_DoesNotAddDuplicateRequiredAttribute_ForIsRequiredTrue()
    {
        // Arrange
        var provider = new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);

        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(DummyRequiredAttributeHelperClass),
            nameof(DummyRequiredAttributeHelperClass.ValueTypeWithoutAttribute));

        var items = GetValidatorItems(metadata);
        var expectedValidatorItem = new ClientValidatorItem
        {
            Validator = new RequiredAttributeAdapter(new RequiredAttribute(), stringLocalizer: null),
            IsReusable = true
        };
        items.Add(expectedValidatorItem);

        var providerContext = new ClientValidatorProviderContext(metadata, items);

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = Assert.Single(providerContext.Results);
        Assert.Same(expectedValidatorItem.Validator, validatorItem.Validator);
    }

    [Fact]
    public void CreateValidators_DoesNotAddRequiredAttribute_ForIsRequiredFalse()
    {
        // Arrange
        var provider = new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);

        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(DummyRequiredAttributeHelperClass),
            nameof(DummyRequiredAttributeHelperClass.ReferenceTypeWithoutAttribute));

        var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        Assert.Empty(providerContext.Results);
    }

    [Fact]
    public void CreateValidators_DoesNotAddExtraRequiredAttribute_IfAttributeIsSpecifiedExplicitly()
    {
        // Arrange
        var provider = new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);

        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(DummyRequiredAttributeHelperClass),
            nameof(DummyRequiredAttributeHelperClass.WithAttribute));

        var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = Assert.Single(providerContext.Results);
        var adapter = Assert.IsType<RequiredAttributeAdapter>(validatorItem.Validator);
        Assert.Equal("Custom Required Message", adapter.Attribute.ErrorMessage);
    }

    [Fact]
    public void UnknownValidationAttribute_IsNotAddedAsValidator()
    {
        // Arrange
        var provider = new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
        var metadata = _metadataProvider.GetMetadataForType(typeof(DummyClassWithDummyValidationAttribute));

        var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = Assert.Single(providerContext.Results);
        Assert.Null(validatorItem.Validator);
    }

    private IList<ClientValidatorItem> GetValidatorItems(ModelMetadata metadata)
    {
        return metadata.ValidatorMetadata.Select(v => new ClientValidatorItem(v)).ToList();
    }

    private class DummyValidationAttribute : ValidationAttribute
    {
    }

    [DummyValidation]
    private class DummyClassWithDummyValidationAttribute
    {
    }

    private class DummyRequiredAttributeHelperClass
    {
        [Required(ErrorMessage = "Custom Required Message")]
        public int WithAttribute { get; set; }

        public int ValueTypeWithoutAttribute { get; set; }

        public string ReferenceTypeWithoutAttribute { get; set; }
    }
}
