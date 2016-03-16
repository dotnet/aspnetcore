// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class FormFileModelBinderTest
    {
        [Fact]
        public async Task FormFileModelBinder_SuppressesValidation()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("file", "file1.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);

            var entry = bindingContext.ValidationState[result.Model];
            Assert.True(entry.SuppressValidation);
            Assert.Equal("file", entry.Key);
            Assert.Null(entry.Metadata);
        }

        [Fact]
        public async Task FormFileModelBinder_ExpectMultipleFiles_BindSuccessful()
        {
            // Arrange
            var formFiles = GetTwoFiles();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);

            var entry = bindingContext.ValidationState[result.Model];
            Assert.True(entry.SuppressValidation);
            Assert.Equal("file", entry.Key);
            Assert.Null(entry.Metadata);

            var files = Assert.IsAssignableFrom<IList<IFormFile>>(result.Model);
            Assert.Equal(2, files.Count);
        }

        [Theory]
        [InlineData(typeof(IFormFile[]))]
        [InlineData(typeof(ICollection<IFormFile>))]
        [InlineData(typeof(IList<IFormFile>))]
        [InlineData(typeof(IFormFileCollection))]
        [InlineData(typeof(List<IFormFile>))]
        [InlineData(typeof(LinkedList<IFormFile>))]
        [InlineData(typeof(FileList))]
        [InlineData(typeof(FormFileCollection))]
        public async Task FormFileModelBinder_BindsFiles_ForCollectionsItCanCreate(Type destinationType)
        {
            // Arrange
            var binder = new FormFileModelBinder();
            var formFiles = GetTwoFiles();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(destinationType, httpContext);

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.IsAssignableFrom(destinationType, result.Model);
            Assert.Equal(formFiles, result.Model as IEnumerable<IFormFile>);
        }

        [Fact]
        public async Task FormFileModelBinder_ExpectSingleFile_BindFirstFile()
        {
            // Arrange
            var formFiles = GetTwoFiles();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            var file = Assert.IsAssignableFrom<IFormFile>(result.Model);
            Assert.Equal("file1.txt", file.FileName);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_WhenNoFilePosted()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_WhenNamesDoNotMatch()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("different name", "file1.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Theory]
        [InlineData(true, "FieldName")]
        [InlineData(false, "ModelName")]
        public async Task FormFileModelBinder_UsesFieldNameForTopLevelObject(bool isTopLevel, string expected)
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("FieldName", "file1.txt"));
            formFiles.Add(GetMockFormFile("ModelName", "file1.txt"));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));

            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            bindingContext.IsTopLevelObject = isTopLevel;
            bindingContext.FieldName = "FieldName";
            bindingContext.ModelName = "ModelName";

            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            var file = Assert.IsAssignableFrom<IFormFile>(result.Model);

            Assert.Equal(expected, file.Name);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_WithEmptyContentDisposition()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(new Mock<IFormFile>().Object);
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_WithNoFileNameAndZeroLength()
        {
            // Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(GetMockFormFile("file", ""));
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
            var binder = new FormFileModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_ForReadOnlyDestination()
        {
            // Arrange
            var binder = new FormFileModelBinder();
            var formFiles = GetTwoFiles();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContextForReadOnlyArray(httpContext);

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task FormFileModelBinder_ReturnsFailedResult_ForCollectionsItCannotCreate()
        {
            // Arrange
            var binder = new FormFileModelBinder();
            var formFiles = GetTwoFiles();
            var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
            var bindingContext = GetBindingContext(typeof(ISet<IFormFile>), httpContext);

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
        }

        private static DefaultModelBindingContext GetBindingContextForReadOnlyArray(HttpContext httpContext)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<ModelWithReadOnlyArray>(nameof(ModelWithReadOnlyArray.ArrayProperty))
                .BindingDetails(bd => bd.BindingSource = BindingSource.Header);
            var modelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithReadOnlyArray),
                nameof(ModelWithReadOnlyArray.ArrayProperty));

            return GetBindingContext(metadataProvider, modelMetadata, httpContext);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType, HttpContext httpContext)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(modelType);

            return GetBindingContext(metadataProvider, metadata, httpContext);
        }

        private static DefaultModelBindingContext GetBindingContext(
            IModelMetadataProvider metadataProvider,
            ModelMetadata metadata,
            HttpContext httpContext)
        {
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadata,
                ModelName = "file",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = httpContext,
                    },
                    MetadataProvider = metadataProvider,
                },
                ValidationState = new ValidationStateDictionary(),
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

        private static FormFileCollection GetTwoFiles()
        {
            var formFiles = new FormFileCollection
            {
                GetMockFormFile("file", "file1.txt"),
                GetMockFormFile("file", "file2.txt"),
            };

            return formFiles;
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
            formFile.Setup(f => f.Name).Returns(modelName);
            formFile.Setup(f => f.FileName).Returns(filename);

            return formFile.Object;
        }

        private class ModelWithReadOnlyArray
        {
            public IFormFile[] ArrayProperty { get; }
        }

        private class FileList : List<IFormFile>
        {
        }
    }
}
