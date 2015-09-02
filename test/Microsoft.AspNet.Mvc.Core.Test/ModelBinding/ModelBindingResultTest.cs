// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
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
        public async Task SuccessAsync_SetsProperties()
        {
            // Arrange
            var key = "someName";
            var model = "some model";

            // Act
            var result = await ModelBindingResult.SuccessAsync(key, model);

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

        [Fact]
        public async Task FailedAsync_SetsProperties()
        {
            // Arrange
            var key = "someName";

            // Act
            var result = await ModelBindingResult.FailedAsync(key);

            // Assert
            Assert.Same(key, result.Key);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public void NoResult_SetsProperties()
        {
            // Arrange & Act
            var result = ModelBindingResult.NoResult;

            // Assert
            Assert.Null(result.Key);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task NoResultAsync_SetsProperties()
        {
            // Arrange & Act
            var result = await ModelBindingResult.NoResultAsync;

            // Assert
            Assert.Null(result.Key);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }
    }
}
