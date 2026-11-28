// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms;

public class BrowserFileTest
{
    [Fact]
    public void SetSize_ThrowsIfSizeIsNegative()
    {
        // Arrange
        var file = new BrowserFile();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => file.Size = -7);
    }

    [Fact]
    public void OpenReadStream_ThrowsIfFileSizeIsLargerThanAllowedSize()
    {
        // Arrange
        var file = new BrowserFile { Size = 100 };

        // Act & Assert
        var ex = Assert.Throws<IOException>(() => file.OpenReadStream(80));
        Assert.Equal("Supplied file with size 100 bytes exceeds the maximum of 80 bytes.", ex.Message);
    }

    [Fact]
    public void OpenReadStream_ReturnsStreamWhoseDisposalReleasesTheJSObject()
    {
        // Arrange: JS runtime that always returns a specific mock IJSStreamReference
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        var jsStreamReference = new Mock<IJSStreamReference>();
        jsRuntime.Setup(x => x.InvokeAsync<IJSStreamReference>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .Returns(ValueTask.FromResult(jsStreamReference.Object));

        // Arrange: InputFile
        var inputFile = new InputFile { JSRuntime = jsRuntime.Object };
        var file = new BrowserFile { Owner = inputFile, Size = 5 };
        var stream = file.OpenReadStream();

        // Assert 1: IJSStreamReference isn't disposed yet
        jsStreamReference.Verify(x => x.DisposeAsync(), Times.Never);

        // Act
        _ = stream.DisposeAsync();

        // Assert: IJSStreamReference is disposed now
        jsStreamReference.Verify(x => x.DisposeAsync());
    }

    [Fact]
    public async Task OpenReadStream_ReturnsStreamWhoseDisposalReleasesTheJSObject_ToleratesDisposalException()
    {
        // Arrange: JS runtime that always returns a specific mock IJSStreamReference whose disposal throws
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        var jsStreamReference = new Mock<IJSStreamReference>();
        jsRuntime.Setup(x => x.InvokeAsync<IJSStreamReference>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .Returns(ValueTask.FromResult(jsStreamReference.Object));
        jsStreamReference.Setup(x => x.DisposeAsync()).Throws(new InvalidTimeZoneException());

        // Arrange: InputFile
        var inputFile = new InputFile { JSRuntime = jsRuntime.Object };
        var file = new BrowserFile { Owner = inputFile, Size = 5 };
        var stream = file.OpenReadStream();

        // Act/Assert. Not throwing is success here.
        await stream.DisposeAsync();
    }

    [Fact]
    public void GeneratedMetadata_DeserializesInputFilePayloadLikeReflection()
    {
        var json = """[{"id":1,"lastModified":"2024-01-02T03:04:05.000Z","name":"a.txt","size":5,"contentType":"text/plain","relativePath":"dir/a.txt","blob":{}}]""";

        var generated = JsonSerializer.Deserialize(json, (JsonTypeInfo<BrowserFile[]>)CreateRuntimeOptions(InputFileJsonContext.Default).GetTypeInfo(typeof(BrowserFile[])))!;
        var reflection = JsonSerializer.Deserialize<BrowserFile[]>(json, CreateRuntimeOptions(new DefaultJsonTypeInfoResolver()))!;

        var file = Assert.Single(generated);
        var expected = Assert.Single(reflection);
        Assert.Equal((1, "a.txt", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), 5L, "text/plain", "dir/a.txt"), (file.Id, file.Name, file.LastModified, file.Size, file.ContentType, file.RelativePath));
        Assert.Equal((expected.Id, expected.Name, expected.LastModified, expected.Size, expected.ContentType, expected.RelativePath), (file.Id, file.Name, file.LastModified, file.Size, file.ContentType, file.RelativePath));
    }

    // Matches the naming options of JSRuntime.
    private static JsonSerializerOptions CreateRuntimeOptions(IJsonTypeInfoResolver resolver)
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = resolver,
        };
}
