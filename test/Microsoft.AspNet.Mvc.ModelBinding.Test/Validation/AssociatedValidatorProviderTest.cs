// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if NET45
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
                    .Returns(() => null)
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
                    .Returns(() => null)
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
                    .Returns(() => null)
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
