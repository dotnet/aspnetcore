// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class CompositeModelValidatorProviderTest
    {
        [Fact]
        public void GetModelValidators_ReturnsValidatorsFromAllProviders()
        {
            // Arrange
            var validator1 = Mock.Of<IModelValidator>();
            var validator2 = Mock.Of<IModelValidator>();
            var validator3 = Mock.Of<IModelValidator>();

            var provider1 = new Mock<IModelValidatorProvider>();
            provider1.Setup(p => p.GetValidators(It.IsAny<ModelValidatorProviderContext>()))
                     .Callback<ModelValidatorProviderContext>(c =>
                     {
                         c.Validators.Add(validator1);
                         c.Validators.Add(validator2);
                     });

            var provider2 = new Mock<IModelValidatorProvider>();
            provider2.Setup(p => p.GetValidators(It.IsAny<ModelValidatorProviderContext>()))
                     .Callback<ModelValidatorProviderContext>(c =>
                     {
                         c.Validators.Add(validator3);
                     });

            var compositeModelValidator = new CompositeModelValidatorProvider(new[] { provider1.Object, provider2.Object });
            var modelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));

            // Act
            var validatorProviderContext = new ModelValidatorProviderContext(modelMetadata);
            compositeModelValidator.GetValidators(validatorProviderContext);

            // Assert
            Assert.Equal(
                new[] { validator1, validator2, validator3 },
                validatorProviderContext.Validators.ToArray());
        }
    }
}
#endif
