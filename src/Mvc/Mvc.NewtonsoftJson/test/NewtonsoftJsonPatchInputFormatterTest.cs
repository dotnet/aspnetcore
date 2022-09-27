// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class NewtonsoftJsonPatchInputFormatterTest
{
    private static readonly ObjectPoolProvider _objectPoolProvider = new DefaultObjectPoolProvider();
    private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings();

    [Fact]
    public async Task Constructor_BuffersRequestBody_ByDefault()
    {
        // Arrange
        var formatter = new NewtonsoftJsonPatchInputFormatter(
            GetLogger(),
            _serializerSettings,
            ArrayPool<char>.Shared,
            _objectPoolProvider,
            new MvcOptions(),
            new MvcNewtonsoftJsonOptions());

        var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes, allowSyncReads: false);
        httpContext.Request.ContentType = "application/json";

        var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
        Assert.Equal("add", patchDocument.Operations[0].op);
        Assert.Equal("Customer/Name", patchDocument.Operations[0].path);
        Assert.Equal("John", patchDocument.Operations[0].value);
    }

    [Fact]
    public async Task Constructor_SuppressInputFormatterBuffering_DoesNotBufferRequestBody()
    {
        // Arrange
        var mvcOptions = new MvcOptions()
        {
            SuppressInputFormatterBuffering = false,
        };
        var formatter = new NewtonsoftJsonPatchInputFormatter(
            GetLogger(),
            _serializerSettings,
            ArrayPool<char>.Shared,
            _objectPoolProvider,
            mvcOptions,
            new MvcNewtonsoftJsonOptions());

        var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
        httpContext.Request.ContentType = "application/json";

        var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

        // Act
        // Mutate options after passing into the constructor to make sure that the value type is not store in the constructor
        mvcOptions.SuppressInputFormatterBuffering = true;
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);

        var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
        Assert.Equal("add", patchDocument.Operations[0].op);
        Assert.Equal("Customer/Name", patchDocument.Operations[0].path);
        Assert.Equal("John", patchDocument.Operations[0].value);

        Assert.False(httpContext.Request.Body.CanSeek);
        result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task JsonPatchInputFormatter_ReadsOneOperation_Successfully()
    {
        // Arrange
        var formatter = CreateFormatter();

        var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = CreateHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
        Assert.Equal("add", patchDocument.Operations[0].op);
        Assert.Equal("Customer/Name", patchDocument.Operations[0].path);
        Assert.Equal("John", patchDocument.Operations[0].value);
    }

    [Fact]
    public async Task JsonPatchInputFormatter_ReadsMultipleOperations_Successfully()
    {
        // Arrange
        var formatter = CreateFormatter();

        var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}," +
            "{\"op\": \"remove\", \"path\" : \"Customer/Name\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = CreateHttpContext(contentBytes);

        var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.False(result.HasError);
        var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
        Assert.Equal("add", patchDocument.Operations[0].op);
        Assert.Equal("Customer/Name", patchDocument.Operations[0].path);
        Assert.Equal("John", patchDocument.Operations[0].value);
        Assert.Equal("remove", patchDocument.Operations[1].op);
        Assert.Equal("Customer/Name", patchDocument.Operations[1].path);
    }

    [Theory]
    [InlineData("application/json-patch+json", true)]
    [InlineData("application/json", false)]
    [InlineData("application/*", false)]
    [InlineData("*/*", false)]
    public void CanRead_ReturnsTrueOnlyForJsonPatchContentType(string requestContentType, bool expectedCanRead)
    {
        // Arrange
        var formatter = CreateFormatter();

        var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = CreateHttpContext(contentBytes, contentType: requestContentType);

        var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

        // Act
        var result = formatter.CanRead(formatterContext);

        // Assert
        Assert.Equal(expectedCanRead, result);
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(IJsonPatchDocument))]
    public void CanRead_ReturnsFalse_NonJsonPatchContentType(Type modelType)
    {
        // Arrange
        var formatter = CreateFormatter();

        var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = CreateHttpContext(contentBytes, contentType: "application/json-patch+json");

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);
        var formatterContext = CreateInputFormatterContext(modelType, httpContext);

        // Act
        var result = formatter.CanRead(formatterContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task JsonPatchInputFormatter_ReturnsModelStateErrors_InvalidModelType()
    {
        // Arrange
        var exceptionMessage = "Cannot deserialize the current JSON array (e.g. [1,2,3]) into type " +
            $"'{typeof(Customer).FullName}' because the type requires a JSON object ";

        // This test relies on 2.1 error message behavior
        var formatter = CreateFormatter(allowInputFormatterExceptionMessages: true);

        var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var httpContext = CreateHttpContext(contentBytes, contentType: "application/json-patch+json");

        var formatterContext = CreateInputFormatterContext(typeof(Customer), httpContext);

        // Act
        var result = await formatter.ReadAsync(formatterContext);

        // Assert
        Assert.True(result.HasError);
        Assert.Contains(exceptionMessage, formatterContext.ModelState[""].Errors[0].ErrorMessage);
    }

    private static ILogger GetLogger()
    {
        return NullLogger.Instance;
    }

    private NewtonsoftJsonPatchInputFormatter CreateFormatter(bool allowInputFormatterExceptionMessages = false)
    {
        return new NewtonsoftJsonPatchInputFormatter(
            NullLogger.Instance,
            _serializerSettings,
            ArrayPool<char>.Shared,
            _objectPoolProvider,
            new MvcOptions(),
            new MvcNewtonsoftJsonOptions()
            {
                AllowInputFormatterExceptionMessages = allowInputFormatterExceptionMessages,
            });
    }

    private InputFormatterContext CreateInputFormatterContext(Type modelType, HttpContext httpContext)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);

        return new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);
    }

    private static HttpContext CreateHttpContext(
        byte[] contentBytes,
        string contentType = "application/json-patch+json")
    {
        var request = new Mock<HttpRequest>();
        var headers = new Mock<IHeaderDictionary>();
        request.SetupGet(r => r.Headers).Returns(headers.Object);
        request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
        request.SetupGet(f => f.ContentType).Returns(contentType);
        request.SetupGet(f => f.ContentLength).Returns(contentBytes.Length);

        var httpContext = new Mock<HttpContext>();
        var features = new Mock<IFeatureCollection>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Features).Returns(features.Object);
        return httpContext.Object;
    }

    private class Customer
    {
        public string Name { get; set; }
    }

    private class TestResponseFeature : HttpResponseFeature
    {
        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            // do not do anything
        }
    }
}
