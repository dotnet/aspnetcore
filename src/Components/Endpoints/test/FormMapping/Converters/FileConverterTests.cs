// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class FileConverterTests
{
    [Fact]
    public void TryRead_IBrowserFile_WithMissingLastModifiedHeader_ReturnsMinValue()
    {
        // Arrange
        const string prefixName = "file";
        var culture = CultureInfo.GetCultureInfo("en-US");
        
        var mockFormFile = CreateMockFormFile();
        var formFileCollection = new Mock<IFormFileCollection>();
        formFileCollection.Setup(x => x.GetFile(prefixName)).Returns(mockFormFile.Object);

        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(new Dictionary<FormKey, StringValues>(), culture, buffer, formFileCollection.Object);
        reader.PushPrefix(prefixName);

        var converter = new FileConverter<IBrowserFile>();

        // Act
        var result = converter.TryRead(ref reader, typeof(IBrowserFile), default!, out var browserFile, out var found);

        // Assert
        Assert.True(result);
        Assert.True(found);
        Assert.NotNull(browserFile);
        Assert.Equal(DateTimeOffset.MinValue, browserFile.LastModified);
    }

    [Fact]
    public void TryRead_IBrowserFile_WithMalformedLastModifiedHeader_ReturnsMinValue()
    {
        // Arrange
        const string prefixName = "file";
        var culture = CultureInfo.GetCultureInfo("en-US");
        
        var mockFormFile = CreateMockFormFile(lastModified: "invalid-date");
        var formFileCollection = new Mock<IFormFileCollection>();
        formFileCollection.Setup(x => x.GetFile(prefixName)).Returns(mockFormFile.Object);

        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(new Dictionary<FormKey, StringValues>(), culture, buffer, formFileCollection.Object);
        reader.PushPrefix(prefixName);

        var converter = new FileConverter<IBrowserFile>();

        // Act
        var result = converter.TryRead(ref reader, typeof(IBrowserFile), default!, out var browserFile, out var found);

        // Assert
        Assert.True(result);
        Assert.True(found);
        Assert.NotNull(browserFile);
        Assert.Equal(DateTimeOffset.MinValue, browserFile.LastModified);
    }

    [Fact]
    public void TryRead_IBrowserFile_WithValidLastModifiedHeader_ReturnsCorrectValue()
    {
        // Arrange
        const string prefixName = "file";
        var culture = CultureInfo.GetCultureInfo("en-US");
        var expectedDate = new DateTimeOffset(2023, 11, 30, 12, 0, 0, TimeSpan.Zero);
        
        var mockFormFile = CreateMockFormFile(lastModified: expectedDate.ToString("r"));
        var formFileCollection = new Mock<IFormFileCollection>();
        formFileCollection.Setup(x => x.GetFile(prefixName)).Returns(mockFormFile.Object);

        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(new Dictionary<FormKey, StringValues>(), culture, buffer, formFileCollection.Object);
        reader.PushPrefix(prefixName);

        var converter = new FileConverter<IBrowserFile>();

        // Act
        var result = converter.TryRead(ref reader, typeof(IBrowserFile), default!, out var browserFile, out var found);

        // Assert
        Assert.True(result);
        Assert.True(found);
        Assert.NotNull(browserFile);
        Assert.Equal(expectedDate, browserFile.LastModified);
    }

    [Fact]
    public void TryRead_IBrowserFile_WithEmptyLastModifiedHeader_ReturnsMinValue()
    {
        // Arrange
        const string prefixName = "file";
        var culture = CultureInfo.GetCultureInfo("en-US");
        
        var mockFormFile = CreateMockFormFile(lastModified: string.Empty);
        var formFileCollection = new Mock<IFormFileCollection>();
        formFileCollection.Setup(x => x.GetFile(prefixName)).Returns(mockFormFile.Object);

        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(new Dictionary<FormKey, StringValues>(), culture, buffer, formFileCollection.Object);
        reader.PushPrefix(prefixName);

        var converter = new FileConverter<IBrowserFile>();

        // Act
        var result = converter.TryRead(ref reader, typeof(IBrowserFile), default!, out var browserFile, out var found);

        // Assert
        Assert.True(result);
        Assert.True(found);
        Assert.NotNull(browserFile);
        Assert.Equal(DateTimeOffset.MinValue, browserFile.LastModified);
    }

    private static Mock<IFormFile> CreateMockFormFile(string? lastModified = null!)
    {
        var mockFormFile = new Mock<IFormFile>();
        var mockHeaders = new Mock<IHeaderDictionary>();
        
        mockHeaders.Setup(x => x.LastModified).Returns(new StringValues(lastModified));
        mockFormFile.Setup(x => x.Headers).Returns(mockHeaders.Object);
        mockFormFile.Setup(x => x.Name).Returns("testfile.txt");
        mockFormFile.Setup(x => x.ContentType).Returns("text/plain");
        mockFormFile.Setup(x => x.Length).Returns(1024);
        mockFormFile.Setup(x => x.OpenReadStream()).Returns(new MemoryStream());

        return mockFormFile;
    }
}