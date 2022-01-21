// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class FormFileModelBinderTest
{
    [Fact]
    public async Task FormFileModelBinder_SingleFile_BindSuccessful()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("file", "file1.txt")
            };
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal("file", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_SingleFileAtTopLevel_BindSuccessfully_WithEmptyModelName()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("file", "file1.txt")
            };

        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Mimic ParameterBinder overwriting ModelName on top level model. In this top-level binding case,
        // FormFileModelBinder uses FieldName from the get-go. (OriginalModelName will be checked but ignored.)
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext { HttpContext = httpContext },
            Mock.Of<IValueProvider>(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(IFormFile)),
            bindingInfo: null,
            modelName: "file");
        bindingContext.ModelName = string.Empty;

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal("file", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_SingleFileWithinTopLevelPoco_BindSuccessfully()
    {
        // Arrange
        const string propertyName = nameof(NestedFormFiles.Files);
        var formFiles = new FormFileCollection
            {
                GetMockFormFile($"{propertyName}", "file1.txt")
            };

        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // In this non-top-level binding case, FormFileModelBinder tries ModelName and succeeds.
        var propertyInfo = typeof(NestedFormFiles).GetProperty(propertyName);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForProperty(
            propertyInfo,
            propertyInfo.PropertyType);
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext { HttpContext = httpContext },
            Mock.Of<IValueProvider>(),
            metadata,
            bindingInfo: null,
            modelName: "FileList");
        bindingContext.IsTopLevelObject = false;
        bindingContext.Model = new FileList();
        bindingContext.ModelName = propertyName;

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal($"{propertyName}", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_SingleFileWithinTopLevelPoco_BindSuccessfully_WithShortenedModelName()
    {
        // Arrange
        const string propertyName = nameof(NestedFormFiles.Files);
        var formFiles = new FormFileCollection
            {
                GetMockFormFile($"FileList.{propertyName}", "file1.txt")
            };

        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Mimic ParameterBinder overwriting ModelName on top level model then ComplexTypeModelBinder entering a
        // nested context for the NestedFormFiles property. In this non-top-level binding case, FormFileModelBinder
        // tries ModelName then falls back to add an (OriginalModelName + ".") prefix.
        var propertyInfo = typeof(NestedFormFiles).GetProperty(propertyName);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForProperty(
            propertyInfo,
            propertyInfo.PropertyType);
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext { HttpContext = httpContext },
            Mock.Of<IValueProvider>(),
            metadata,
            bindingInfo: null,
            modelName: "FileList");
        bindingContext.IsTopLevelObject = false;
        bindingContext.Model = new FileList();
        bindingContext.ModelName = propertyName;

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal($"FileList.{propertyName}", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_SingleFileWithinTopLevelDictionary_BindSuccessfully()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("[myFile]", "file1.txt")
            };

        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // In this non-top-level binding case, FormFileModelBinder tries ModelName and succeeds.
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext { HttpContext = httpContext },
            Mock.Of<IValueProvider>(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(IFormFile)),
            bindingInfo: null,
            modelName: "FileDictionary");
        bindingContext.IsTopLevelObject = false;
        bindingContext.ModelName = "[myFile]";

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal("[myFile]", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_SingleFileWithinTopLevelDictionary_BindSuccessfully_WithShortenedModelName()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("FileDictionary[myFile]", "file1.txt")
            };

        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Mimic ParameterBinder overwriting ModelName on top level model then DictionaryModelBinder entering a
        // nested context for the KeyValuePair.Value property. In this non-top-level binding case,
        // FormFileModelBinder tries ModelName then falls back to add an OriginalModelName prefix.
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext { HttpContext = httpContext },
            Mock.Of<IValueProvider>(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(IFormFile)),
            bindingInfo: null,
            modelName: "FileDictionary");
        bindingContext.IsTopLevelObject = false;
        bindingContext.ModelName = "[myFile]";

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal("FileDictionary[myFile]", entry.Key);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public async Task FormFileModelBinder_ExpectMultipleFiles_BindSuccessful()
    {
        // Arrange
        var formFiles = GetTwoFiles();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IEnumerable<IFormFile>), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.False(entry.SuppressValidation);
        Assert.Equal("file", entry.Key);
        Assert.Null(entry.Metadata);

        var files = Assert.IsAssignableFrom<IList<IFormFile>>(bindingContext.Result.Model);
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
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);
        var formFiles = GetTwoFiles();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(destinationType, httpContext);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.IsAssignableFrom(destinationType, bindingContext.Result.Model);
        Assert.Equal(formFiles, bindingContext.Result.Model as IEnumerable<IFormFile>);
    }

    [Fact]
    public async Task FormFileModelBinder_ExpectSingleFile_BindFirstFile()
    {
        // Arrange
        var formFiles = GetTwoFiles();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var file = Assert.IsAssignableFrom<IFormFile>(bindingContext.Result.Model);
        Assert.Equal("file1.txt", file.FileName);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsFailedResult_WhenNoFilePosted()
    {
        // Arrange
        var formFiles = new FormFileCollection();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsFailedResult_WhenNamesDoNotMatch()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("different name", "file1.txt")
            };
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(true, "FieldName")]
    [InlineData(false, "ModelName")]
    public async Task FormFileModelBinder_UsesFieldNameForTopLevelObject(bool isTopLevel, string expected)
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("FieldName", "file1.txt"),
                GetMockFormFile("ModelName", "file1.txt")
            };
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));

        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        bindingContext.IsTopLevelObject = isTopLevel;
        bindingContext.FieldName = "FieldName";
        bindingContext.ModelName = "ModelName";

        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var file = Assert.IsAssignableFrom<IFormFile>(bindingContext.Result.Model);

        Assert.Equal(expected, file.Name);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsFailedResult_WithEmptyContentDisposition()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                new Mock<IFormFile>().Object
            };
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsFailedResult_WithNoFileNameAndZeroLength()
    {
        // Arrange
        var formFiles = new FormFileCollection
            {
                GetMockFormFile("file", "")
            };
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(IFormFile), httpContext);
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsResult_ForReadOnlyDestination()
    {
        // Arrange
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);
        var formFiles = GetTwoFiles();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContextForReadOnlyArray(httpContext);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.NotNull(bindingContext.Result.Model);
    }

    [Fact]
    public async Task FormFileModelBinder_ReturnsFailedResult_ForCollectionsItCannotCreate()
    {
        // Arrange
        var binder = new FormFileModelBinder(NullLoggerFactory.Instance);
        var formFiles = GetTwoFiles();
        var httpContext = GetMockHttpContext(GetMockFormCollection(formFiles));
        var bindingContext = GetBindingContext(typeof(ISet<IFormFile>), httpContext);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    private static DefaultModelBindingContext GetBindingContextForReadOnlyArray(HttpContext httpContext)
    {
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty<ModelWithReadOnlyArray>(nameof(ModelWithReadOnlyArray.ArrayProperty))
            .BindingDetails(bd => bd.BindingSource = BindingSource.Header);
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithReadOnlyArray),
            nameof(ModelWithReadOnlyArray.ArrayProperty));

        return GetBindingContext(metadata, httpContext);
    }

    private static DefaultModelBindingContext GetBindingContext(Type modelType, HttpContext httpContext)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(modelType);

        return GetBindingContext(metadata, httpContext);
    }

    private static DefaultModelBindingContext GetBindingContext(
        ModelMetadata metadata,
        HttpContext httpContext)
    {
        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = httpContext,
            },
            ModelMetadata = metadata,
            ModelName = "file",
            ModelState = new ModelStateDictionary(),
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

    private class NestedFormFiles
    {
        public FileList Files { get; }
    }
}
