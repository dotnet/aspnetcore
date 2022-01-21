// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ClientValidatorCacheTest
{
    [Fact]
    public void GetValidators_CachesAllValidators()
    {
        // Arrange
        var cache = new ClientValidatorCache();
        var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
        var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

        // Act - 1
        var validators1 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 1
        var attribute1 = Assert.Single(validators1.OfType<RequiredAttributeAdapter>()).Attribute;
        var attribute2 = Assert.Single(validators1.OfType<StringLengthAttributeAdapter>()).Attribute;
        Assert.Contains(attribute1, metadata.ValidatorMetadata); // Copied by provider
        Assert.Contains(attribute2, metadata.ValidatorMetadata); // Copied by provider

        // Act - 2
        var validators2 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 2
        Assert.Same(validators1, validators2);

        Assert.Contains(validators1[0], validators2); // Cached
        Assert.Contains(validators1[1], validators2); // Cached
    }

    [Fact]
    public void GetValidators_DoesNotCacheValidatorsWithIsReusableFalse()
    {
        // Arrange
        var cache = new ClientValidatorCache();
        var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
        var validatorProvider = new ProviderWithNonReusableValidators();

        // Act - 1
        var validators1 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 1
        var validator1 = Assert.Single(validators1.OfType<RequiredAttributeAdapter>());
        var validator2 = Assert.Single(validators1.OfType<StringLengthAttributeAdapter>());
        Assert.Contains(validator1.Attribute, metadata.ValidatorMetadata); // Copied by provider
        Assert.Contains(validator2.Attribute, metadata.ValidatorMetadata); // Copied by provider

        // Act - 2
        var validators2 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 2
        Assert.NotSame(validators1, validators2);

        Assert.Same(validator1, Assert.Single(validators2.OfType<RequiredAttributeAdapter>())); // cached
        Assert.NotSame(validator2, Assert.Single(validators2.OfType<StringLengthAttributeAdapter>())); // not cached
    }

    [Fact]
    public void GetValidators_ReadsValidatorsFromCorrespondingRecordTypeParameter()
    {
        // Arrange
        var cache = new ClientValidatorCache();
        var modelMetadataProvider = new TestModelMetadataProvider();
        var metadata = modelMetadataProvider.GetMetadataForType(typeof(TestRecordType));
        var property = metadata.Properties[nameof(TestRecordType.Property1)];
        var parameter = metadata.BoundConstructor.BoundConstructorParameters.First(f => f.Name == nameof(TestRecordType.Property1));
        var validatorProvider = new ProviderWithNonReusableValidators();

        // Act
        var validators = cache.GetValidators(property, validatorProvider);

        // Assert
        var validator1 = Assert.Single(validators.OfType<RequiredAttributeAdapter>());
        var validator2 = Assert.Single(validators.OfType<StringLengthAttributeAdapter>());
        Assert.Contains(validator1.Attribute, parameter.ValidatorMetadata); // Copied by provider
        Assert.Contains(validator2.Attribute, parameter.ValidatorMetadata); // Copied by provider
    }

    [Fact]
    public void GetValidators_ReadsValidatorsFromProperty_IfRecordTypeDoesNotHaveCorrespondingParameter()
    {
        // Arrange
        var cache = new ClientValidatorCache();
        var modelMetadataProvider = new TestModelMetadataProvider();
        var metadata = modelMetadataProvider.GetMetadataForType(typeof(TestRecordTypeWithProperty));
        var property = metadata.Properties[nameof(TestRecordTypeWithProperty.Property2)];
        var validatorProvider = new ProviderWithNonReusableValidators();

        // Act
        var validators = cache.GetValidators(property, validatorProvider);

        // Assert
        var validator1 = Assert.Single(validators.OfType<RequiredAttributeAdapter>());
        var validator2 = Assert.Single(validators.OfType<StringLengthAttributeAdapter>());
        Assert.Contains(validator1.Attribute, property.ValidatorMetadata); // Copied by provider
        Assert.Contains(validator2.Attribute, property.ValidatorMetadata); // Copied by provider
    }

    private class TypeWithProperty
    {
        [Required]
        [StringLength(10)]
        public string Property1 { get; set; }
    }

    private record TestRecordType([Required][StringLength(10)] string Property1);

    private record TestRecordTypeWithProperty([Required][StringLength(10)] string Property1)
    {
        [Required]
        [StringLength(10)]
        public string Property2 { get; set; }
    }

    private class ProviderWithNonReusableValidators : IClientModelValidatorProvider
    {
        public void CreateValidators(ClientValidatorProviderContext context)
        {
            for (var i = 0; i < context.Results.Count; i++)
            {
                var validatorItem = context.Results[i];
                if (validatorItem.Validator != null)
                {
                    continue;
                }

                var attribute = validatorItem.ValidatorMetadata as ValidationAttribute;
                if (attribute == null)
                {
                    continue;
                }

                var validationAdapterProvider = new ValidationAttributeAdapterProvider();

                validatorItem.Validator = validationAdapterProvider.GetAttributeAdapter(attribute, stringLocalizer: null);

                if (attribute is RequiredAttribute)
                {
                    validatorItem.IsReusable = true;
                }
            }
        }
    }
}
