// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class DataAnnotationsModelValidatorProviderTest
{
    private readonly IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

    [Fact]
    public void CreateValidators_ReturnsValidatorForIValidatableObject()
    {
        // Arrange
        var provider = new DataAnnotationsModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
        var mockValidatable = Mock.Of<IValidatableObject>();
        var metadata = _metadataProvider.GetMetadataForType(mockValidatable.GetType());

        var providerContext = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = Assert.Single(providerContext.Results);
        Assert.IsType<ValidatableObjectAdapter>(validatorItem.Validator);
    }

    [Fact]
    public void CreateValidators_InsertsRequiredValidatorsFirst()
    {
        var provider = new DataAnnotationsModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
        var metadata = _metadataProvider.GetMetadataForProperty(
            typeof(ClassWithProperty),
            "PropertyWithMultipleValidationAttributes");

        var providerContext = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        Assert.Equal(4, providerContext.Results.Count);
        Assert.IsAssignableFrom<RequiredAttribute>(((DataAnnotationsModelValidator)providerContext.Results[0].Validator).Attribute);
        Assert.IsAssignableFrom<RequiredAttribute>(((DataAnnotationsModelValidator)providerContext.Results[1].Validator).Attribute);
    }

    [Fact]
    public void UnknownValidationAttributeGetsDefaultAdapter()
    {
        // Arrange
        var provider = new DataAnnotationsModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
        var metadata = _metadataProvider.GetMetadataForType(typeof(DummyClassWithDummyValidationAttribute));

        var providerContext = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        var validatorItem = providerContext.Results.Single();
        Assert.IsType<DataAnnotationsModelValidator>(validatorItem.Validator);
    }

    private class DummyValidationAttribute : ValidationAttribute
    {
    }

    [DummyValidation]
    private class DummyClassWithDummyValidationAttribute
    {
    }

    // Default IValidatableObject adapter factory

    [Fact]
    public void IValidatableObjectGetsAValidator()
    {
        // Arrange
        var provider = new DataAnnotationsModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
        var mockValidatable = new Mock<IValidatableObject>();
        var metadata = _metadataProvider.GetMetadataForType(mockValidatable.Object.GetType());

        var providerContext = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

        // Act
        provider.CreateValidators(providerContext);

        // Assert
        Assert.Single(providerContext.Results);
    }

    [Fact]
    public void HasValidators_ReturnsTrue_IfModelIsIValidatableObject()
    {
        // Arrange
        var provider = GetProvider();
        var mockValidatable = Mock.Of<IValidatableObject>();

        // Act
        var result = provider.HasValidators(mockValidatable.GetType(), Array.Empty<object>());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidators_ReturnsTrue_IfMetadataContainsValidationAttribute()
    {
        // Arrange
        var provider = GetProvider();
        var attributes = new object[] { new BindNeverAttribute(), new DummyValidationAttribute() };

        // Act
        var result = provider.HasValidators(typeof(object), attributes);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidators_ReturnsFalse_IfNoDataAnnotationsValidationIsAvailable()
    {
        // Arrange
        var provider = GetProvider();
        var attributes = new object[] { new BindNeverAttribute(), };

        // Act
        var result = provider.HasValidators(typeof(object), attributes);

        // Assert
        Assert.False(result);
    }

    private static DataAnnotationsModelValidatorProvider GetProvider()
    {
        return new DataAnnotationsModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            Options.Create(new MvcDataAnnotationsLocalizationOptions()),
            stringLocalizerFactory: null);
    }

    private IList<ValidatorItem> GetValidatorItems(ModelMetadata metadata)
    {
        var items = new List<ValidatorItem>(metadata.ValidatorMetadata.Count);
        for (var i = 0; i < metadata.ValidatorMetadata.Count; i++)
        {
            items.Add(new ValidatorItem(metadata.ValidatorMetadata[i]));
        }

        return items;
    }

    private class ObservableModel
    {
        private bool _propertyWasRead;

        public string TheProperty
        {
            get
            {
                _propertyWasRead = true;
                return "Hello";
            }
        }

        public bool PropertyWasRead()
        {
            return _propertyWasRead;
        }
    }

    private class BaseModel
    {
        public virtual string MyProperty { get; set; }
    }

    private class DerivedModel : BaseModel
    {
        [StringLength(10)]
        public override string MyProperty
        {
            get { return base.MyProperty; }
            set { base.MyProperty = value; }
        }
    }

    private class DummyRequiredAttributeHelperClass
    {
        [Required(ErrorMessage = "Custom Required Message")]
        public int WithAttribute { get; set; }

        public int WithoutAttribute { get; set; }
    }

    private class ClassWithProperty
    {
        [CustomNonRequiredAttribute1]
        [CustomNonRequiredAttribute2]
        [CustomRequiredAttribute1]
        [CustomRequiredAttribute2]
        public string PropertyWithMultipleValidationAttributes { get; set; }
    }

    public class CustomRequiredAttribute1 : RequiredAttribute
    {
    }

    public class CustomRequiredAttribute2 : RequiredAttribute
    {
    }

    public class CustomNonRequiredAttribute1 : ValidationAttribute
    {
    }

    public class CustomNonRequiredAttribute2 : ValidationAttribute
    {
    }
}
