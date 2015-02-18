// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class HeaderModelBinderTests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(string[]))]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(TestFromHeader))]
        public async Task BindModelAsync_ReturnsTrue_ForAllTypes(Type type)
        {
            // Arrange
            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HeaderBinder_BindsHeaders_ToStringCollection()
        {
            // Arrange
            var type = typeof(string[]);
            var header = "Accept";
            var headerValue = "application/json,text/json";
            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);

            modelBindingContext.ModelName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(headerValue.Split(','), result.Model);
        }

        [Fact]
        public async Task HeaderBinder_BindsHeaders_ToStringType()
        {
            // Arrange
            var type = typeof(string);
            var header = "User-Agent";
            var headerValue = "UnitTest";
            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);

            modelBindingContext.ModelName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(headerValue, result.Model);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "modelName",
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = new HeaderModelBinder(),
                    MetadataProvider = metadataProvider,
                    HttpContext = new DefaultHttpContext()
                }
            };

            bindingContext.ModelMetadata.BinderMetadata = new TestFromHeader();
            return bindingContext;
        }

        public class TestFromHeader : IBindingSourceMetadata
        {
            public BindingSource BindingSource { get; } = BindingSource.Header;
        }
    }
}