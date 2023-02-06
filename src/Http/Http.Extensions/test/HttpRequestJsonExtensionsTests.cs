// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

#nullable enable

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class HttpRequestJsonExtensionsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("application/xml", false)]
    [InlineData("text/json", false)]
    [InlineData("text/json; charset=utf-8", false)]
    [InlineData("application/json", true)]
    [InlineData("application/json; charset=utf-8", true)]
    [InlineData("application/ld+json", true)]
    [InlineData("APPLICATION/JSON", true)]
    [InlineData("APPLICATION/JSON; CHARSET=UTF-8", true)]
    [InlineData("APPLICATION/LD+JSON", true)]
    public void HasJsonContentType(string contentType, bool hasJsonContentType)
    {
        var request = new DefaultHttpContext().Request;
        request.ContentType = contentType;

        Assert.Equal(hasJsonContentType, request.HasJsonContentType());
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_NonJsonContentType_ThrowError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "text/json";

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Request.ReadFromJsonAsync<int>());

        // Assert
        var expectedMessage = $"Unable to read the request as JSON because the request content type 'text/json' is not a known JSON content type.";
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_NoBodyContent_ThrowError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        // Act
        var ex = await Assert.ThrowsAsync<JsonException>(async () => await context.Request.ReadFromJsonAsync<int>());

        // Assert
        var expectedMessage = $"The input does not contain any JSON tokens. Expected the input to start with a valid JSON token, when isFinalBlock is true. Path: $ | LineNumber: 0 | BytePositionInLine: 0.";
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_ValidBodyContent_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("1"));

        // Act
        var result = await context.Request.ReadFromJsonAsync<int>();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_WithOptions_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("[1,2,]"));

        var options = new JsonSerializerOptions();
        options.AllowTrailingCommas = true;

        // Act
        var result = await context.Request.ReadFromJsonAsync<List<int>>(options);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_Utf8Encoding_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=utf-8";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("[1,2]"));

        // Act
        var result = await context.Request.ReadFromJsonAsync<List<int>>();

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_Utf16Encoding_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=utf-16";
        context.Request.Body = new MemoryStream(Encoding.Unicode.GetBytes(@"{""name"": ""激光這兩個字是甚麼意思""}"));

        // Act
        var result = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();

        // Assert
        Assert.Equal("激光這兩個字是甚麼意思", result!["name"]);
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_WithCancellationToken_CancellationRaised()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application /json";
        context.Request.Body = new TestStream();

        var cts = new CancellationTokenSource();

        // Act
        var readTask = context.Request.ReadFromJsonAsync<List<int>>(cts.Token);
        Assert.False(readTask.IsCompleted);

        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await readTask);
    }

    [Fact]
    public async Task ReadFromJsonAsyncGeneric_InvalidEncoding_ThrowError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=invalid";

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Request.ReadFromJsonAsync<object>());

        // Assert
        Assert.Equal("Unable to read the request as JSON because the request content type charset 'invalid' is not a known encoding.", ex.Message);
    }

    [Fact]
    public async Task ReadFromJsonAsync_ValidBodyContent_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("1"));

        // Act
        var result = (int?)await context.Request.ReadFromJsonAsync(typeof(int));

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ReadFromJsonAsync_Utf16Encoding_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=utf-16";
        context.Request.Body = new MemoryStream(Encoding.Unicode.GetBytes(@"{""name"": ""激光這兩個字是甚麼意思""}"));

        // Act
        var result = (Dictionary<string, string>?)await context.Request.ReadFromJsonAsync(typeof(Dictionary<string, string>));

        // Assert
        Assert.Equal("激光這兩個字是甚麼意思", result!["name"]);
    }

    [Fact]
    public async Task ReadFromJsonAsync_InvalidEncoding_ThrowError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json; charset=invalid";

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Request.ReadFromJsonAsync(typeof(object)));

        // Assert
        Assert.Equal("Unable to read the request as JSON because the request content type charset 'invalid' is not a known encoding.", ex.Message);
    }

    [Fact]
    public async Task ReadFromJsonAsync_WithOptions_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("[1,2,]"));

        var options = new JsonSerializerOptions();
        options.AllowTrailingCommas = true;

        // Act
        var result = (List<int>?)await context.Request.ReadFromJsonAsync(typeof(List<int>), options);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
    }

    [Fact]
    public async Task ReadFromJsonAsync_WithTypeInfo_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("[1,2,]"));

        var options = new JsonSerializerOptions();
        options.AllowTrailingCommas = true;
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        // Act
        var result = (List<int>?)await context.Request.ReadFromJsonAsync(options.GetTypeInfo(typeof(List<int>)));

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
    }

    [Fact]
    public async Task ReadFromJsonAsync_WithGenericTypeInfo_ReturnValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("[1,2,]"));

        var options = new JsonSerializerOptions();
        options.AllowTrailingCommas = true;
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        // Act
        var typeInfo = (JsonTypeInfo<List<int>>)options.GetTypeInfo(typeof(List<int>));
        var result = await context.Request.ReadFromJsonAsync(typeInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(result,
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i));
    }
}
