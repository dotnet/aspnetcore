// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class CompositeModelValidatorProviderTest
    {
        [Fact]
        public void GetModelValidators_ReturnsValidatorsFromAllProviders()
        {
            // Arrange
            var validatorMetadata = new object();
            var validator1 = new ValidatorItem(validatorMetadata);
            var validator2 = new ValidatorItem(validatorMetadata);
            var validator3 = new ValidatorItem(validatorMetadata);

            var provider1 = new Mock<IModelValidatorProvider>();
            provider1.Setup(p => p.CreateValidators(It.IsAny<ModelValidatorProviderContext>()))
                     .Callback<ModelValidatorProviderContext>(c =>
                     {
                         c.Results.Add(validator1);
                         c.Results.Add(validator2);
                     });

            var provider2 = new Mock<IModelValidatorProvider>();
            provider2.Setup(p => p.CreateValidators(It.IsAny<ModelValidatorProviderContext>()))
                     .Callback<ModelValidatorProviderContext>(c =>
                     {
                         c.Results.Add(validator3);
                     });

            var compositeModelValidator = new CompositeModelValidatorProvider(new[] { provider1.Object, provider2.Object });
            var modelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));

            // Act
            var validatorProviderContext = new ModelValidatorProviderContext(modelMetadata, new List<ValidatorItem>());
            compositeModelValidator.CreateValidators(validatorProviderContext);

            // Assert
            Assert.Equal(
                new[] { validator1, validator2, validator3 },
                validatorProviderContext.Results.ToArray());
        }
    }
}
