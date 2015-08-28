// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageModelBinderTest
    {
        [Fact]
        public async Task BindModelAsync_ReturnsNotNull_ForHttpRequestMessageType()
        {
            // Arrange
            var binder = new HttpRequestMessageModelBinder();
            var bindingContext = GetBindingContext(typeof(HttpRequestMessage));
            var expectedModel = bindingContext.OperationBindingContext.HttpContext.GetHttpRequestMessage();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Same(expectedModel, result.Model);
            Assert.NotNull(result.ValidationNode);
            Assert.True(result.ValidationNode.SuppressValidation);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(object))]
        [InlineData(typeof(HttpRequestMessageModelBinderTest))]
        public async Task BindModelAsync_ReturnsNull_ForNonHttpRequestMessageType(Type type)
        {
            // Arrange
            var binder = new HttpRequestMessageModelBinder();
            var bindingContext = GetBindingContext(type);

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
                OperationBindingContext = new OperationBindingContext
                {
                    HttpContext = new DefaultHttpContext(),
                    MetadataProvider = metadataProvider,
                }
            };

            bindingContext.OperationBindingContext.HttpContext.Request.Method = "GET";

            return bindingContext;
        }
    }
}
