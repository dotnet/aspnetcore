// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class BrowserFileFromFormFileTests
{
    [Fact]
    public void Name_ReturnsFormFileFileName_NotFormFileName()
    {
        // Arrange
        const string expectedFileName = "document.pdf";
        const string formFieldName = "fileInput";
        
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(x => x.FileName).Returns(expectedFileName);
        mockFormFile.Setup(x => x.Name).Returns(formFieldName);
        
        var browserFile = new BrowserFileFromFormFile(mockFormFile.Object);
        
        // Act
        var actualName = browserFile.Name;
        
        // Assert
        Assert.Equal(expectedFileName, actualName);
        Assert.NotEqual(formFieldName, actualName);
    }

    [Fact]
    public void TryRead_IBrowserFile_ReturnsCorrectFileName()
    {
        // Arrange
        const string prefixName = "fileInput";
        const string expectedFileName = "upload.txt";
        var culture = CultureInfo.GetCultureInfo("en-US");
        
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(x => x.FileName).Returns(expectedFileName);
        mockFormFile.Setup(x => x.Name).Returns(prefixName);
        
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
        Assert.Equal(expectedFileName, browserFile.Name);
    }

    [Fact]
    public void TryRead_IBrowserFileList_ReturnsCorrectFileNames()
    {
        // Arrange
        const string prefixName = "fileInputs";
        const string expectedFileName1 = "file1.txt";
        const string expectedFileName2 = "file2.jpg";
        var culture = CultureInfo.GetCultureInfo("en-US");
        
        var mockFormFile1 = new Mock<IFormFile>();
        mockFormFile1.Setup(x => x.FileName).Returns(expectedFileName1);
        mockFormFile1.Setup(x => x.Name).Returns(prefixName);
        
        var mockFormFile2 = new Mock<IFormFile>();
        mockFormFile2.Setup(x => x.FileName).Returns(expectedFileName2);
        mockFormFile2.Setup(x => x.Name).Returns(prefixName);
        
        var formFiles = new List<IFormFile> { mockFormFile1.Object, mockFormFile2.Object };
        var formFileCollection = new Mock<IFormFileCollection>();
        formFileCollection.Setup(x => x.GetFiles(prefixName)).Returns(formFiles);

        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(new Dictionary<FormKey, StringValues>(), culture, buffer, formFileCollection.Object);
        reader.PushPrefix(prefixName);

        var converter = new FileConverter<IReadOnlyList<IBrowserFile>>();

        // Act
        var result = converter.TryRead(ref reader, typeof(IReadOnlyList<IBrowserFile>), default!, out var browserFiles, out var found);

        // Assert
        Assert.True(result);
        Assert.True(found);
        Assert.NotNull(browserFiles);
        Assert.Equal(2, browserFiles.Count);
        Assert.Equal(expectedFileName1, browserFiles[0].Name);
        Assert.Equal(expectedFileName2, browserFiles[1].Name);
    }
}