// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
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
            provider1.Setup(p => p.GetValidators(It.IsAny<ModelMetadata>()))
                     .Returns(new[] { validator1, validator2 });
            var provider2 = new Mock<IModelValidatorProvider>();
            provider2.Setup(p => p.GetValidators(It.IsAny<ModelMetadata>()))
                     .Returns(new[] { validator3 });
            var providerProvider = new Mock<IModelValidatorProviderProvider>();
            providerProvider.Setup(p => p.ModelValidatorProviders)
                            .Returns(new[] { provider1.Object, provider2.Object });
            var compositeModelValidator = new CompositeModelValidatorProvider(providerProvider.Object);
            var modelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(
                                    modelAccessor: null,
                                    modelType: typeof(string));

            // Act
            var result = compositeModelValidator.GetValidators(modelMetadata);

            // Assert
            Assert.Equal(new[] { validator1, validator2, validator3 }, result);
        }
    }
}
#endif
