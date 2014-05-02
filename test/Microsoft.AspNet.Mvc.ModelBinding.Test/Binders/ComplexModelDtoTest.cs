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
            return new ModelMetadata(new EmptyModelMetadataProvider(), typeof(object), null, typeof(object), "PropertyName");
        }
    }
}
