// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CancellationTokenModelBinderTests
    {
        [Fact]
        public async Task CancellationTokenModelBinder_ReturnsNonEmptyResult_ForCancellationTokenType()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(CancellationToken));
            var binder = new CancellationTokenModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal(bindingContext.OperationBindingContext.HttpContext.RequestAborted, result.Model);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(object))]
        [InlineData(typeof(CancellationTokenModelBinderTests))]
        public async Task CancellationTokenModelBinder_ReturnsNull_ForNonCancellationTokenType(Type t)
        {
            // Arrange
            var bindingContext = GetBindingContext(t);
            var binder = new CancellationTokenModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            ModelBindingContext bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = new SimpleValueProvider(),
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = new CancellationTokenModelBinder(),
                    MetadataProvider = metadataProvider,
                    HttpContext = new DefaultHttpContext(),
                }
            };

            return bindingContext;
        }
    }
}
