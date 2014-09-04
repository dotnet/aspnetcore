// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class AssociatedValidatorProviderTest
    {
        private readonly DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void GetValidatorsForPropertyWithLocalAttributes()
        {
            // Arrange
            IEnumerable<Attribute> callbackAttributes = null;
            var metadata = _metadataProvider.GetMetadataForProperty(null, typeof(PropertyModel), "LocalAttributes");
            var provider = new Mock<TestableAssociatedValidatorProvider> { CallBase = true };
            provider.Setup(p => p.AbstractGetValidators(metadata, It.IsAny<IEnumerable<Attribute>>()))
                    .Callback<ModelMetadata, IEnumerable<Attribute>>((m, attributes) => callbackAttributes = attributes)
                    .Returns((IEnumerable<IModelValidator>)null)
                    .Verifiable();

            // Act
            provider.Object.GetValidators(metadata);

            // Assert
            provider.Verify();
            Assert.True(callbackAttributes.Any(a => a is RequiredAttribute));
        }

        [Fact]
        public void GetValidatorsForPropertyWithMetadataAttributes()
        {
            // Arrange
            IEnumerable<Attribute> callbackAttributes = null;
            var metadata = _metadataProvider.GetMetadataForProperty(null, typeof(PropertyModel), "MetadataAttributes");
            Mock<TestableAssociatedValidatorProvider> provider = new Mock<TestableAssociatedValidatorProvider> { CallBase = true };
            provider.Setup(p => p.AbstractGetValidators(metadata, It.IsAny<IEnumerable<Attribute>>()))
                    .Callback<ModelMetadata, IEnumerable<Attribute>>((m, attributes) => callbackAttributes = attributes)
                    .Returns((IEnumerable<IModelValidator>)null)
                    .Verifiable();

            // Act
            provider.Object.GetValidators(metadata);

            // Assert
            provider.Verify();
            Assert.True(callbackAttributes.Any(a => a is RangeAttribute));
        }

        [Fact]
        public void GetValidatorsForPropertyWithMixedAttributes()
        {
            // Arrange
            IEnumerable<Attribute> callbackAttributes = null;
            var metadata = _metadataProvider.GetMetadataForProperty(null, typeof(PropertyModel), "MixedAttributes");
            Mock<TestableAssociatedValidatorProvider> provider = new Mock<TestableAssociatedValidatorProvider> { CallBase = true };
            provider.Setup(p => p.AbstractGetValidators(metadata, It.IsAny<IEnumerable<Attribute>>()))
                    .Callback<ModelMetadata, IEnumerable<Attribute>>((m, attributes) => callbackAttributes = attributes)
                    .Returns((IEnumerable<IModelValidator>)null)
                    .Verifiable();

            // Act
            provider.Object.GetValidators(metadata);

            // Assert
            provider.Verify();
            Assert.True(callbackAttributes.Any(a => a is RangeAttribute));
            Assert.True(callbackAttributes.Any(a => a is RequiredAttribute));
        }

        private class PropertyModel
        {
            [Required]
            public int LocalAttributes { get; set; }

            [Range(10, 100)]
            public string MetadataAttributes { get; set; }

            [Required]
            [Range(10, 100)]
            public double MixedAttributes { get; set; }
        }

        public abstract class TestableAssociatedValidatorProvider : AssociatedValidatorProvider
        {
            protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata, IEnumerable<Attribute> attributes)
            {
                return AbstractGetValidators(metadata, attributes);
            }

            public abstract IEnumerable<IModelValidator> AbstractGetValidators(ModelMetadata metadata, IEnumerable<Attribute> attributes);
        }
    }
}
#endif
