// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
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
            var bindingContext = GetBindingContext(type);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
        }

        [Fact]
        public async Task HeaderBinder_BindsHeaders_ToStringCollection()
        {
            // Arrange
            var type = typeof(string[]);
            var header = "Accept";
            var headerValue = "application/json,text/json";
            var binder = new HeaderModelBinder();
            var bindingContext = GetBindingContext(type);

            bindingContext.FieldName = header;
            bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Equal(headerValue.Split(','), bindingContext.Result.Model);
        }

        [Fact]
        public async Task HeaderBinder_BindsHeaders_ToStringType()
        {
            // Arrange
            var type = typeof(string);
            var header = "User-Agent";
            var headerValue = "UnitTest";
            var binder = new HeaderModelBinder();
            var bindingContext = GetBindingContext(type);

            bindingContext.FieldName = header;
            bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Equal(headerValue, bindingContext.Result.Model);
        }

        [Theory]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(ICollection<string>))]
        [InlineData(typeof(IList<string>))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(LinkedList<string>))]
        [InlineData(typeof(StringList))]
        public async Task HeaderBinder_BindsHeaders_ForCollectionsItCanCreate(Type destinationType)
        {
            // Arrange
            var header = "Accept";
            var headerValue = "application/json,text/json";
            var binder = new HeaderModelBinder();
            var bindingContext = GetBindingContext(destinationType);

            bindingContext.FieldName = header;
            bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsAssignableFrom(destinationType, bindingContext.Result.Model);
            Assert.Equal(headerValue.Split(','), bindingContext.Result.Model as IEnumerable<string>);
        }

        [Fact]
        public async Task HeaderBinder_ReturnsResult_ForReadOnlyDestination()
        {
            // Arrange
            var header = "Accept";
            var headerValue = "application/json,text/json";
            var binder = new HeaderModelBinder();
            var bindingContext = GetBindingContextForReadOnlyArray();

            bindingContext.FieldName = header;
            bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.NotNull(bindingContext.Result.Model);
        }

        [Fact]
        public async Task HeaderBinder_ReturnsFailedResult_ForCollectionsItCannotCreate()
        {
            // Arrange
            var header = "Accept";
            var headerValue = "application/json,text/json";
            var binder = new HeaderModelBinder();
            var bindingContext = GetBindingContext(typeof(ISet<string>));

            bindingContext.FieldName = header;
            bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Header);
            var modelMetadata = metadataProvider.GetMetadataForType(modelType);

            return GetBindingContext(metadataProvider, modelMetadata);
        }

        private static DefaultModelBindingContext GetBindingContextForReadOnlyArray()
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<ModelWithReadOnlyArray>(nameof(ModelWithReadOnlyArray.ArrayProperty))
                .BindingDetails(bd => bd.BindingSource = BindingSource.Header);
            var modelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithReadOnlyArray),
                nameof(ModelWithReadOnlyArray.ArrayProperty));

            return GetBindingContext(metadataProvider, modelMetadata);
        }

        private static DefaultModelBindingContext GetBindingContext(
            IModelMetadataProvider metadataProvider,
            ModelMetadata modelMetadata)
        {
            var bindingContext = new DefaultModelBindingContext
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
                ModelMetadata = modelMetadata,
                ModelName = "modelName",
                FieldName = "modelName",
                ModelState = new ModelStateDictionary(),
                BinderModelName = modelMetadata.BinderModelName,
                BindingSource = modelMetadata.BindingSource,
            };

            return bindingContext;
        }

        private class ModelWithReadOnlyArray
        {
            public string[] ArrayProperty { get; }
        }

        private class StringList : List<string>
        {
        }
    }
}