// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
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

        private class TypeWithProperty
        {
            [Required]
            [StringLength(10)]
            public string Property1 { get; set; }
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
}