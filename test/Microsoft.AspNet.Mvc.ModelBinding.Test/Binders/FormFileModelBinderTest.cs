// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core.Collections;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormFileModelBinderTest
    {
        [Fact]
        public async Task FormFileModelBinder_ExpectMultipleFiles_BindSuccessful()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("file", "file1.txt"));
            formFiles.Add(GetMockFormFile("file", "file2.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var files = Assert.IsAssignableFrom<IList<IFormFile>>(result.Model);
            Assert.Equal(2, files.Count);
        }

        [Fact]
        public async Task FormFileModelBinder_FilesWithQuotedContentDisposition_BindSuccessful()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFileWithQuotedContentDisposition("file", "file1.txt"));
            formFiles.Add(GetMockFormFileWithQuotedContentDisposition("file", "file2.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var files = Assert.IsAssignableFrom<IList<IFormFile>>(result.Model);
            Assert.Equal(2, files.Count);
        }

        [Fact]
        public async Task FormFileModelBinder_ExpectSingleFile_BindFirstFile()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("file", "file1.txt"));
            formFiles.Add(GetMockFormFile("file", "file2.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var file = Assert.IsAssignableFrom<IFormFile>(result.Model);
            Assert.Equal("form-data; name=file; filename=file1.txt",
                         file.ContentDisposition);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsNull_WhenNoFilePosted()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsNull_WhenNamesDontMatch()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("different name", "file1.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsNull_WithEmptyContentDisposition()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(new Mock<IFormFile>().Object);
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsNull_WithNoFileNameAndZeroLength()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("file", ""));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
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
                    ModelBinder = new FormFileModelBinder(),
                    MetadataProvider = metadataProvider,
                    HttpContext = httpContext,
                }
            };

            return bindingContext;
        }

        private static HttpContext GetMockHttpContext(IFormCollection formCollection)
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(h => h.Request.ReadFormAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(formCollection));
            httpContext.Setup(h => h.Request.HasFormContentType).Returns(true);
            return httpContext.Object;
        }

        private static IFormCollection GetMockFormCollection(FormFileCollection formFiles)
        {
            var formCollection = new Mock<IFormCollection>();
            formCollection.Setup(f => f.Files).Returns(formFiles);
            return formCollection.Object;
        }

        private static IFormFile GetMockFormFile(string modelName, string filename)
        {
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.ContentDisposition)
                .Returns(string.Format("form-data; name={0}; filename={1}", modelName, filename));
            return formFile.Object;
        }

        private static IFormFile GetMockFormFileWithQuotedContentDisposition(string modelName, string filename)
        {
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.ContentDisposition)
                .Returns(string.Format("form-data; name=\"{0}\"; filename=\"{1}\"", modelName, filename));
            return formFile.Object;
        }
    }
}

#endif