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

using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ComplexModelDtoResultTest
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            // Arrange
            var validationNode = GetValidationNode();

            // Act
            var result = new ComplexModelDtoResult("some string", validationNode);

            // Assert
            Assert.Equal("some string", result.Model);
            Assert.Equal(validationNode, result.ValidationNode);
        }

        private static ModelValidationNode GetValidationNode()
        {
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(null, typeof(object));
            return new ModelValidationNode(metadata, "someKey");
    }
}
}
