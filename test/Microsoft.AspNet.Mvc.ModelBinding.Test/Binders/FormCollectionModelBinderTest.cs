// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core.Collections;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormCollectionModelBinderTest
    {
        [Fact]
        public async Task FormCollectionModelBinder_ValidType_BindSuccessful()
        {
            // Arrange
            var formCollection = new FormCollection(new Dictionary<string, string[]>
            {
                { "field1", new string[] { "value1" } },
                { "field2", new string[] { "value2" } }
            });
            var httpContext = GetMockHttpContext(formCollection);
            var bindingContext = GetBindingContext(typeof(FormCollection), httpContext);
            var binder = new FormCollectionModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var form = Assert.IsAssignableFrom<IFormCollection>(result.Model);
            Assert.Equal(2, form.Count);
            Assert.Equal("value1", form["field1"]);
            Assert.Equal("value2", form["field2"]);
        }

        [Fact]
        public async Task FormCollectionModelBinder_InvalidType_BindFails()
        {
            // Arrange
            var formCollection = new FormCollection(new Dictionary<string, string[]>
            {
                { "field1", new string[] { "value1" } },
                { "field2", new string[] { "value2" } }
            });
            var httpContext = GetMockHttpContext(formCollection);
            var bindingContext = GetBindingContext(typeof(string), httpContext);
            var binder = new FormCollectionModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FormCollectionModelBinder_NoForm_BindSuccessful_ReturnsEmptyFormCollection()
        {
            // Arrange
            var httpContext = GetMockHttpContext(null, hasForm: false);
            var bindingContext = GetBindingContext(typeof(IFormCollection), httpContext);
            var binder = new FormCollectionModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.IsType(typeof(FormCollection), result.Model);
            Assert.Empty((FormCollection)result.Model);
        }

        [Fact]
        public async Task FormCollectionModelBinder_CustomFormCollection_BindSuccessful()
        {
            // Arrange
            var formCollection = new MyFormCollection(new Dictionary<string, string[]>
            {
                { "field1", new string[] { "value1" } },
                { "field2", new string[] { "value2" } }
            });
            var httpContext = GetMockHttpContext(formCollection);
            var bindingContext = GetBindingContext(typeof(FormCollection), httpContext);
            var binder = new FormCollectionModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var form = Assert.IsAssignableFrom<IFormCollection>(result.Model);
            Assert.Equal(2, form.Count);
            Assert.Equal("value1", form["field1"]);
            Assert.Equal("value2", form["field2"]);
        }

        private static HttpContext GetMockHttpContext(IFormCollection formCollection, bool hasForm = true)
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(h => h.Request.ReadFormAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(formCollection));
            httpContext.Setup(h => h.Request.HasFormContentType).Returns(hasForm);
            return httpContext.Object;
        }

        private static ModelBindingContext GetBindingContext(Type modelType, HttpContext httpContext)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "file",
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = new FormCollectionModelBinder(),
                    MetadataProvider = metadataProvider,
                    HttpContext = httpContext,
                }
            };

            return bindingContext;
        }

        private class MyFormCollection : ReadableStringCollection, IFormCollection
        {
            public MyFormCollection(IDictionary<string, string[]> store) : this(store, new FormFileCollection())
            {
            }

            public MyFormCollection(IDictionary<string, string[]> store, IFormFileCollection files) : base(store)
            {
                Files = files;
            }

            public IFormFileCollection Files { get; private set; }
        }
    }
}

#endif