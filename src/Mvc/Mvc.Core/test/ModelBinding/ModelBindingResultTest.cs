// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class ModelBindingResultTest
    {
        [Fact]
        public void Success_SetsProperties()
        {
            // Arrange
            var model = "some model";

            // Act
            var result = ModelBindingResult.Success(model);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Same(model, result.Model);
        }
        
        [Fact]
        public void Failed_SetsProperties()
        {
            // Arrange & Act
            var result = ModelBindingResult.Failed();

            // Assert
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }
    }
}
