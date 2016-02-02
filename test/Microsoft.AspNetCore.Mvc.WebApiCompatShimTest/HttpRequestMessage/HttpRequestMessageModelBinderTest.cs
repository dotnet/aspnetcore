// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageModelBinderTest
    {
        [Fact]
        public async Task BindModelAsync_ReturnsNonEmptyResult_ForHttpRequestMessageType()
        {
            // Arrange
            var binder = new HttpRequestMessageModelBinder();
            var bindingContext = GetBindingContext(typeof(HttpRequestMessage));
            var expectedModel = bindingContext.OperationBindingContext.HttpContext.GetHttpRequestMessage();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.HasValue);

            var result = bindingContext.Result.Value;
            Assert.True(result.IsModelSet);
            Assert.Same(expectedModel, result.Model);

            var entry = bindingContext.ValidationState[result.Model];
            Assert.True(entry.SuppressValidation);
            Assert.Null(entry.Key);
            Assert.Null(entry.Metadata);
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
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.HasValue);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            DefaultModelBindingContext bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                OperationBindingContext = new OperationBindingContext
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = new DefaultHttpContext(),
                    },
                    MetadataProvider = metadataProvider,
                },
                ValidationState = new ValidationStateDictionary(),
            };

            bindingContext.OperationBindingContext.HttpContext.Request.Method = "GET";

            return bindingContext;
        }
    }
}
