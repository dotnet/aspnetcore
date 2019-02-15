// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    public class DataTypeClientModelValidatorProviderTest
    {
        private readonly IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        [Theory]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(float?))]
        [InlineData(typeof(double?))]
        [InlineData(typeof(decimal?))]
        public void CreateValidators_GetsNumericValidator_ForNumericType(Type modelType)
        {
            // Arrange
            var provider = new NumericClientModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(modelType);

            var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            provider.CreateValidators(providerContext);

            // Assert
            var validatorItem = Assert.Single(providerContext.Results);
            Assert.IsType<NumericClientModelValidator>(validatorItem.Validator);
        }

        [Fact]
        public void CreateValidators_DoesNotAddDuplicateValidators()
        {
            // Arrange
            var provider = new NumericClientModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(typeof(float));
            var items = GetValidatorItems(metadata);
            var expectedValidatorItem = new ClientValidatorItem
            {
                Validator = new NumericClientModelValidator(),
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

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(short))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(uint?))]
        [InlineData(typeof(long?))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void CreateValidators_DoesNotGetsNumericValidator_ForUnsupportedTypes(Type modelType)
        {
            // Arrange
            var provider = new NumericClientModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(modelType);

            var providerContext = new ClientValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            provider.CreateValidators(providerContext);

            // Assert
            Assert.Empty(providerContext.Results);
        }

        private IList<ClientValidatorItem> GetValidatorItems(ModelMetadata metadata)
        {
            return metadata.ValidatorMetadata.Select(v => new ClientValidatorItem(v)).ToList();
        }
    }
}
