// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public abstract class JsonInputFormatterTestBase : LoggedTest
{
    [Theory]
    [InlineData("application/json", true)]
    [InlineData("application/*", false)]
    [InlineData("*/*", false)]
    [InlineData("text/json", true)]
    [InlineData("text/*", false)]
    [InlineData("text/xml", false)]
    [InlineData("application/xml", false)]
    [InlineData("application/some.entity+json", true)]
    [InlineData("application/some.entity+json;v=2", true)]
    [InlineData("application/some.entity+xml", false)]
    [InlineData("application/some.entity+*", false)]
    [InlineData("text/some.entity+json", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("invalid", false)]
    public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
    {
        // Arrange
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes("content");
        var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);

        var formatterContext = CreateInputFormatterContext(typeof(string), httpContext);

        // Act
        var result = formatter.CanRead(formatterContext);

        // Assert
        Assert.Equal(expectedCanRead, result);
    }

    [Fact]
    public void DefaultMediaType_ReturnsApplicationJson()
    {
        // Arrange
        var formatter = GetInputFormatter();

        // Act
        var mediaType = formatter.SupportedMediaTypes[0];

        // Assert
        Assert.Equal("application/json", mediaType.ToString());
    }

    [Fact]
    public async Task JsonFormatterReadsIntValue()
    {
        // Arrange
        var content = "100";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(int), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var intValue = Assert.IsType<int>(result.Model);
        Assert.Equal(100, intValue);
    }

    [Fact]
    public async Task JsonFormatterReadsStringValue()
    {
        // Arrange
        var content = "\"abcd\"";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(string), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var stringValue = Assert.IsType<string>(result.Model);
        Assert.Equal("abcd", stringValue);
    }

    [Fact]
    public async Task JsonFormatterReadsNonUtf8Content()
    {
        // Arrange
        var content = "☀☁☂☃☄★☆☇☈☉☊☋☌☍☎☏☐☑☒☓☚☛☜☝☞☟☠☡☢☣☤☥☦☧☨☩☪☫☬☮☯☰☱☲☳☴☵☶☷☸";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.Unicode.GetBytes($"\"{content}\"");
        var httpContext = GetHttpContext(contentBytes, "application/json;charset=utf-16");

        var formatterContext = CreateInputFormatterContext(typeof(string), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var stringValue = Assert.IsType<string>(result.Model);
        Assert.Equal(content, stringValue);
        Assert.True(httpContext.Request.Body.CanRead, "Verify that the request stream hasn't been disposed");
    }

    [Fact]
    public virtual async Task JsonFormatter_EscapedKeys()
    {
        var expectedKey = JsonFormatter_EscapedKeys_Expected;

        // Arrange
        var content = "[{\"It\\\"s a key\": 1234556}]";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(
            typeof(IEnumerable<IDictionary<string, short>>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
            });
    }

    [Fact]
    public virtual async Task JsonFormatter_EscapedKeys_Bracket()
    {
        var expectedKey = JsonFormatter_EscapedKeys_Bracket_Expected;

        // Arrange
        var content = "[{\"It[s a key\":1234556}]";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(IEnumerable<IDictionary<string, short>>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
            });
    }

    [Fact]
    public virtual async Task JsonFormatter_EscapedKeys_SingleQuote()
    {
        var expectedKey = JsonFormatter_EscapedKeys_SingleQuote_Expected;

        // Arrange
        var content = "[{\"It's a key\": 1234556}]";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(
            typeof(IEnumerable<IDictionary<string, short>>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
            });
    }

    [Fact]
    public virtual async Task JsonFormatterReadsDateTimeValue()
    {
        // Arrange
        var expected = new DateTime(2012, 02, 01, 00, 45, 00);
        var content = $"\"{expected.ToString("O")}\"";
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(DateTime), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var dateValue = Assert.IsType<DateTime>(result.Model);
        Assert.Equal(expected, dateValue);
    }

    [Fact]
    public async Task JsonFormatterReadsComplexTypes()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{\"name\": \"Person Name\", \"age\": 30}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var userModel = Assert.IsType<ComplexModel>(result.Model);
        Assert.Equal("Person Name", userModel.Name);
        Assert.Equal(30, userModel.Age);
    }

    [Fact]
    public async Task ReadAsync_ReadsValidArray()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[0, 23, 300]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(int[]), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var integers = Assert.IsType<int[]>(result.Model);
        Assert.Equal(new int[] { 0, 23, 300 }, integers);
    }

    [Fact]
    public virtual Task ReadAsync_ReadsValidArray_AsListOfT() => ReadAsync_ReadsValidArray_AsList(typeof(List<int>));

    [Fact]
    public virtual Task ReadAsync_ReadsValidArray_AsIListOfT() => ReadAsync_ReadsValidArray_AsList(typeof(IList<int>));

    [Fact]
    public virtual Task ReadAsync_ReadsValidArray_AsCollectionOfT() => ReadAsync_ReadsValidArray_AsList(typeof(ICollection<int>));

    [Fact]
    public virtual Task ReadAsync_ReadsValidArray_AsEnumerableOfT() => ReadAsync_ReadsValidArray_AsList(typeof(IEnumerable<int>));

    protected async Task ReadAsync_ReadsValidArray_AsList(Type requestedType)
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[0, 23, 300]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(requestedType, httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        Assert.IsAssignableFrom(requestedType, result.Model);
        Assert.Equal(new int[] { 0, 23, 300 }, (IEnumerable<int>)result.Model);
    }

    [Fact]
    public virtual async Task ReadAsync_ArrayOfObjects_HasCorrectKey()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[{\"Age\": 5}, {\"Age\": 3}, {\"Age\": \"Cheese\"} ]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(List<ComplexModel>), httpContext);

        var expectedKey = ReadAsync_ArrayOfObjects_HasCorrectKey_Expected;

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have had an error!");
        Assert.Collection(formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
                Assert.Single(kvp.Value.Errors);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_AddsModelValidationErrorsToModelState()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{ \"Name\": \"Person Name\", \"Age\": \"not-an-age\" }";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);
        var expectedKey = ReadAsync_AddsModelValidationErrorsToModelState_Expected;

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have had an error!");
        Assert.Collection(formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
                Assert.Single(kvp.Value.Errors);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[0, 23, 33767]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(short[]), httpContext);

        var expectedValue = ReadAsync_InvalidArray_AddsOverflowErrorsToModelState_Expected;

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have produced an error!");
        Assert.Collection(formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedValue, kvp.Key);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "[{ \"Name\": \"Name One\", \"Age\": 30}, { \"Name\": \"Name Two\", \"Small\": 300}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel[]), httpContext, modelName: "names");
        var expectedKey = ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState_Expected;

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
                Assert.Single(kvp.Value.Errors);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{ \"Name\": \"Person Name\", \"Age\": \"not-an-age\"}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);
        formatterContext.ModelState.MaxAllowedErrors = 3;
        formatterContext.ModelState.AddModelError("key1", "error1");
        formatterContext.ModelState.AddModelError("key2", "error2");

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);

        Assert.False(formatterContext.ModelState.ContainsKey("age"));
        var error = Assert.Single(formatterContext.ModelState[""].Errors);
        Assert.IsType<TooManyModelErrorsException>(error.Exception);
    }

    [Theory]
    [InlineData("null", true, true)]
    [InlineData("null", false, false)]
    public async Task ReadAsync_WithInputThatDeserializesToNull_SetsModelOnlyIfAllowingEmptyInput(
        string content,
        bool treatEmptyInputAsDefaultValue,
        bool expectedIsModelSet)
    {
        // Arrange
        var formatter = GetInputFormatter();

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(
            typeof(string),
            httpContext,
            treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        Assert.Equal(expectedIsModelSet, result.IsModelSet);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task ReadAsync_ComplexPoco()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{ \"Id\": 5, \"Person\": { \"Name\": \"name\", \"Numbers\": [3, 2, \"Hamburger\"]} }";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexPoco), httpContext);

        var expectedKey = ReadAsync_ComplexPoco_Expected;

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have had an error!");
        Assert.Collection(formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(expectedKey, kvp.Key);
                Assert.Single(kvp.Value.Errors);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_NestedParseError()
    {
        // Arrange
        var formatter = GetInputFormatter();
        var content = @"{ ""b"": { ""c"": { ""d"": efg } } }";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(A), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have had an error!");
        Assert.Collection(
            formatterContext.ModelState.OrderBy(k => k.Key),
            kvp =>
            {
                Assert.Equal(ReadAsync_NestedParseError_Expected, kvp.Key);
            });
    }

    [Fact]
    public virtual async Task ReadAsync_RequiredAttribute()
    {
        // Arrange
        var formatter = GetInputFormatter();
        var content = "{ \"Id\": 5, \"Person\": {\"Numbers\": [3]} }";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(ComplexPoco), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError, "Model should have had an error!");
        Assert.Single(formatterContext.ModelState["Person.Name"].Errors);
    }

    [Fact]
    public async Task ReadAsync_DoesNotDisposeBufferedReadStream()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{\"name\": \"Test\"}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);
        var testBufferedReadStream = new VerifyDisposeFileBufferingReadStream(httpContext.Request.Body, 1024);
        httpContext.Request.Body = testBufferedReadStream;

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        var userModel = Assert.IsType<ComplexModel>(result.Model);
        Assert.Equal("Test", userModel.Name);
        Assert.False(testBufferedReadStream.Disposed);
    }

    [Fact]
    public async Task ReadAsync_WithEnableBufferingWorks()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "{\"name\": \"Test\"}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);
        httpContext.Request.EnableBuffering();

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        var userModel = Assert.IsType<ComplexModel>(result.Model);
        Assert.Equal("Test", userModel.Name);
        var requestBody = httpContext.Request.Body;
        requestBody.Position = 0;
        Assert.Equal(content, new StreamReader(requestBody).ReadToEnd());
    }

    [Fact]
    public async Task ReadAsync_WithEnableBufferingWorks_WithInputStreamAtOffset()
    {
        // Arrange
        var formatter = GetInputFormatter();

        var content = "abc{\"name\": \"Test\"}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = GetHttpContext(contentBytes);
        httpContext.Request.EnableBuffering();
        var requestBody = httpContext.Request.Body;
        requestBody.Position = 3;

        var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        var userModel = Assert.IsType<ComplexModel>(result.Model);
        Assert.Equal("Test", userModel.Name);
        requestBody.Position = 0;
        Assert.Equal(content, new StreamReader(requestBody).ReadToEnd());
    }

    internal abstract string JsonFormatter_EscapedKeys_Bracket_Expected { get; }

    internal abstract string JsonFormatter_EscapedKeys_SingleQuote_Expected { get; }

    internal abstract string JsonFormatter_EscapedKeys_Expected { get; }

    internal abstract string ReadAsync_ArrayOfObjects_HasCorrectKey_Expected { get; }

    internal abstract string ReadAsync_AddsModelValidationErrorsToModelState_Expected { get; }

    internal abstract string ReadAsync_InvalidArray_AddsOverflowErrorsToModelState_Expected { get; }

    internal abstract string ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState_Expected { get; }

    internal abstract string ReadAsync_ComplexPoco_Expected { get; }

    internal abstract string ReadAsync_NestedParseError_Expected { get; }

    protected abstract TextInputFormatter GetInputFormatter(bool allowInputFormatterExceptionMessages = true);

    protected static HttpContext GetHttpContext(
        byte[] contentBytes,
        string contentType = "application/json")
    {
        return GetHttpContext(new MemoryStream(contentBytes), contentType);
    }

    protected static HttpContext GetHttpContext(
        Stream requestStream,
        string contentType = "application/json")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = requestStream;
        httpContext.Request.ContentType = contentType;
        httpContext.Request.ContentLength = requestStream.Length;

        return httpContext;
    }

    protected static InputFormatterContext CreateInputFormatterContext(
        Type modelType,
        HttpContext httpContext,
        string modelName = null,
        bool treatEmptyInputAsDefaultValue = false)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);

        return new InputFormatterContext(
            httpContext,
            modelName: modelName ?? string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader,
            treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);
    }

    protected sealed class ComplexPoco
    {
        public int Id { get; set; }
        public Person Person { get; set; }
    }

    protected sealed class Person
    {
        [Required]
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        public IEnumerable<int> Numbers { get; set; }
    }

    protected sealed class ComplexModel
    {
        public string Name { get; set; }

        public decimal Age { get; set; }

        public byte Small { get; set; }
    }

    class A
    {
        public B B { get; set; }
    }

    class B
    {
        public C C { get; set; }
    }

    class C
    {
        public string D { get; set; }
    }

    private class VerifyDisposeFileBufferingReadStream : FileBufferingReadStream
    {
        public bool Disposed { get; private set; }
        public VerifyDisposeFileBufferingReadStream(Stream inner, int memoryThreshold) : base(inner, memoryThreshold)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return base.DisposeAsync();
        }
    }
}
