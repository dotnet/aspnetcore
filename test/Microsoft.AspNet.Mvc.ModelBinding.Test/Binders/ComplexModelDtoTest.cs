// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ComplexModelDtoTest
    {
        [Fact]
        public void ConstructorSetsProperties()
        {
            // Arrange
            ModelMetadata modelMetadata = GetModelMetadata();
            ModelMetadata[] propertyMetadata = new ModelMetadata[0];

            // Act
            ComplexModelDto dto = new ComplexModelDto(modelMetadata, propertyMetadata);

            // Assert
            Assert.Equal(modelMetadata, dto.ModelMetadata);
            Assert.Equal(propertyMetadata, dto.PropertyMetadata.ToArray());
            Assert.Empty(dto.Results);
        }

        private static ModelMetadata GetModelMetadata()
        {
            return new ModelMetadata(new EmptyModelMetadataProvider(), typeof(object), typeof(object), "PropertyName");
        }
    }
}
