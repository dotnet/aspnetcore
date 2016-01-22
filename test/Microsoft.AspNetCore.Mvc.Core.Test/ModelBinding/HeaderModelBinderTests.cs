// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
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
        [InlineData(typeof(BindingSource))]
        public async Task BindModelAsync_ReturnsNonEmptyResult_ForAllTypes_WithHeaderBindingSource(Type type)
        {
            // Arrange
            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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

            modelBindingContext.FieldName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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

            modelBindingContext.FieldName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.Equal(headerValue, result.Model);
        }

        [Fact]
        public async Task HeaderBinder_ReturnsNoResult_ForNullBindingSource()
        {
            // Arrange
            var type = typeof(string);
            var header = "User-Agent";
            var headerValue = "UnitTest";

            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);
            modelBindingContext.BindingSource = null;

            modelBindingContext.FieldName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        [Fact]
        public async Task HeaderBinder_ReturnsNoResult_ForNonHeaderBindingSource()
        {
            // Arrange
            var type = typeof(string);
            var header = "User-Agent";
            var headerValue = "UnitTest";

            var binder = new HeaderModelBinder();
            var modelBindingContext = GetBindingContext(type);
            modelBindingContext.BindingSource = BindingSource.Body;

            modelBindingContext.FieldName = header;
            modelBindingContext.OperationBindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            var result = await binder.BindModelAsync(modelBindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Header);
            var modelMetadata = metadataProvider.GetMetadataForType(modelType);
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = modelMetadata,
                ModelName = "modelName",
                FieldName = "modelName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = new DefaultHttpContext(),
                    },
                    ModelBinder = new HeaderModelBinder(),
                    MetadataProvider = metadataProvider,
                },
                BinderModelName = modelMetadata.BinderModelName,
                BindingSource = modelMetadata.BindingSource,
            };

            return bindingContext;
        }
    }
}