// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
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
        public void GetValidators_GetsNumericValidator_ForNumericType(Type modelType)
        {
            // Arrange
            var provider = new NumericClientModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(modelType);

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            var validator = Assert.Single(providerContext.Validators);
            Assert.IsType<NumericClientModelValidator>(validator);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(short))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(uint?))]
        [InlineData(typeof(long?))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DateTime))]
        public void GetValidators_DoesNotGetsNumericValidator_ForUnsupportedTypes(Type modelType)
        {
            // Arrange
            var provider = new NumericClientModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(modelType);

            var providerContext = new ClientValidatorProviderContext(metadata);

            // Act
            provider.GetValidators(providerContext);

            // Assert
            Assert.Empty(providerContext.Validators);
        }
    }
}
