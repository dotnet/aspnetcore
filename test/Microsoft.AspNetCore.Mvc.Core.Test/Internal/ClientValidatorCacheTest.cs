// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ClientValidatorCacheTest
    {
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void GetValidators_CachesAllValidators()
        {
            // Arrange
            var cache = new ClientValidatorCache();
            var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
            var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

            // Act - 1
            var validators1 = cache.GetValidators(metadata, validatorProvider);

            // Assert - 1
            Assert.Collection(
                validators1,
                v => Assert.Same(metadata.ValidatorMetadata[0], Assert.IsType<RequiredAttributeAdapter>(v).Attribute), // Copied by provider
                v => Assert.Same(metadata.ValidatorMetadata[1], Assert.IsType<StringLengthAttributeAdapter>(v).Attribute)); // Copied by provider

            // Act - 2
            var validators2 = cache.GetValidators(metadata, validatorProvider);

            // Assert - 2
            Assert.Same(validators1, validators2);

            Assert.Collection(
                validators2,
                v => Assert.Same(validators1[0], v), // Cached
                v => Assert.Same(validators1[1], v)); // Cached
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void GetValidators_DoesNotCacheValidatorsWithIsReusableFalse()
        {
            // Arrange
            var cache = new ClientValidatorCache();
            var metadata = new TestModelMetadataProvider().GetMetadataForProperty(typeof(TypeWithProperty), "Property1");
            var validatorProvider = new ProviderWithNonReusableValidators();

            // Act - 1
            var validators1 = cache.GetValidators(metadata, validatorProvider);

            // Assert - 1
            Assert.Collection(
                validators1,
                v => Assert.Same(metadata.ValidatorMetadata[0], Assert.IsType<RequiredAttributeAdapter>(v).Attribute), // Copied by provider
                v => Assert.Same(metadata.ValidatorMetadata[1], Assert.IsType<StringLengthAttributeAdapter>(v).Attribute)); // Copied by provider

            // Act - 2
            var validators2 = cache.GetValidators(metadata, validatorProvider);

            // Assert - 2
            Assert.NotSame(validators1, validators2);

            Assert.Collection(
                validators2,
                v => Assert.Same(validators1[0], v), // Cached
                v => Assert.NotSame(validators1[1], v)); // Not cached
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