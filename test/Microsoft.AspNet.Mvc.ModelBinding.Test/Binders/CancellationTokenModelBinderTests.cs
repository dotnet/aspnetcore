// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CancellationTokenModelBinderTests
    {
        [Fact]
        public async Task CancellationTokenModelBinder_ReturnsTrue_ForCancellationTokenType()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(CancellationToken));
            var binder = new CancellationTokenModelBinder();

            // Act
            var bound = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bound);
            Assert.Equal(bindingContext.HttpContext.RequestAborted, bindingContext.Model);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(object))]
        [InlineData(typeof(CancellationTokenModelBinderTests))]
        public async Task CancellationTokenModelBinder_ReturnsFalse_ForNonCancellationTokenType(Type t)
        {
            // Arrange
            var bindingContext = GetBindingContext(t);
            var binder = new CancellationTokenModelBinder();

            // Act
            var bound = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bound);
            Assert.Null(bindingContext.Model);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            ModelBindingContext bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(null, modelType),
                ModelName = "someName",
                ValueProvider = new SimpleHttpValueProvider(),
                ModelBinder = new CancellationTokenModelBinder(),
                MetadataProvider = metadataProvider,
                HttpContext = new DefaultHttpContext(),
            };

            return bindingContext;
        }
    }
}
