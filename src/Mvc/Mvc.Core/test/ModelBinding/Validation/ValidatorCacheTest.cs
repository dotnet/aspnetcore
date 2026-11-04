// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ValidatorCacheTest
{
    [Fact]
    public void GetValidators_CachesAllValidators()
    {
        // Arrange
        var cache = new ValidatorCache();
        var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
        var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

        // Act - 1
        var validators1 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 1
        var attribute1 = Assert.IsType<DataAnnotationsModelValidator>(validators1[0]).Attribute;
        var attribute2 = Assert.IsType<DataAnnotationsModelValidator>(validators1[1]).Attribute;
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
        var cache = new ValidatorCache();
        var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
        var validatorProvider = new ProviderWithNonReusableValidators();

        // Act - 1
        var validators1 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 1
        var validator1 = Assert.IsType<DataAnnotationsModelValidator>(validators1[0]);
        var validator2 = Assert.IsType<DataAnnotationsModelValidator>(validators1[1]);
        Assert.Contains(validator1.Attribute, metadata.ValidatorMetadata); // Copied by provider
        Assert.Contains(validator2.Attribute, metadata.ValidatorMetadata); // Copied by provider

        // Act - 2
        var validators2 = cache.GetValidators(metadata, validatorProvider);

        // Assert - 2
        Assert.NotSame(validators1, validators2);

        var requiredValidator = Assert.Single(validators2.Where(v => (v as DataAnnotationsModelValidator).Attribute is RequiredAttribute));
        Assert.Contains(requiredValidator, validators1); // cached
        var stringLengthValidator = Assert.Single(validators2.Where(v => (v as DataAnnotationsModelValidator).Attribute is StringLengthAttribute));
        Assert.DoesNotContain(stringLengthValidator, validators1); // not cached
    }

    private class TypeWithProperty
    {
        [Required]
        [StringLength(10)]
        public string Property1 { get; set; }
    }

    private class ProviderWithNonReusableValidators : IModelValidatorProvider
    {
        public void CreateValidators(ModelValidatorProviderContext context)
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

                var validator = new DataAnnotationsModelValidator(
                    new ValidationAttributeAdapterProvider(),
                    attribute,
                    stringLocalizer: null);

                validatorItem.Validator = validator;

                if (attribute is RequiredAttribute)
                {
                    validatorItem.IsReusable = true;
                }
            }
        }
    }
}
