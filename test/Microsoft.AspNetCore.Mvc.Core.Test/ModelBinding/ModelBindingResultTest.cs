// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class ModelBindingResultTest
    {
        [Fact]
        public void Success_SetsProperties()
        {
            // Arrange
            var key = "someName";
            var model = "some model";

            // Act
            var result = ModelBindingResult.Success(key, model);

            // Assert
            Assert.Same(key, result.Key);
            Assert.True(result.IsModelSet);
            Assert.Same(model, result.Model);
        }
        
        [Fact]
        public void Failed_SetsProperties()
        {
            // Arrange
            var key = "someName";

            // Act
            var result = ModelBindingResult.Failed(key);

            // Assert
            Assert.Same(key, result.Key);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }
    }
}
