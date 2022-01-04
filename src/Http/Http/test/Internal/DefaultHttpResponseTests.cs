// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Microsoft.AspNetCore.Http;

public class DefaultHttpResponseTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(9001)]
    [InlineData(65535)]
    public void GetContentLength_ReturnsParsedHeader(long value)
    {
        // Arrange
        var response = GetResponseWithContentLength(value.ToString(CultureInfo.InvariantCulture));

        // Act and Assert
        Assert.Equal(value, response.ContentLength);
    }

    [Fact]
    public void GetContentLength_ReturnsNullIfHeaderDoesNotExist()
    {
        // Arrange
        var response = GetResponseWithContentLength(contentLength: null);

        // Act and Assert
        Assert.Null(response.ContentLength);
    }

    [Theory]
    [InlineData("cant-parse-this")]
    [InlineData("-1000")]
    [InlineData("1000.00")]
    [InlineData("100/5")]
    public void GetContentLength_ReturnsNullIfHeaderCannotBeParsed(string contentLength)
    {
        // Arrange
        var response = GetResponseWithContentLength(contentLength);

        // Act and Assert
        Assert.Null(response.ContentLength);
    }

    [Fact]
    public void GetContentType_ReturnsNullIfHeaderDoesNotExist()
    {
        // Arrange
        var response = GetResponseWithContentType(contentType: null);

        // Act and Assert
        Assert.Null(response.ContentType);
    }

    [Fact]
    public void BodyWriter_CanGet()
    {
        var response = new DefaultHttpContext();
        var bodyPipe = response.Response.BodyWriter;

        Assert.NotNull(bodyPipe);
    }

    [Fact]
    public void ReplacingResponseBody_DoesNotCreateOnCompletedRegistration()
    {
        var features = new FeatureCollection();

        var originalStream = new FlushAsyncCheckStream();
        var replacementStream = new FlushAsyncCheckStream();

        var responseBodyMock = new Mock<IHttpResponseBodyFeature>();
        responseBodyMock.Setup(o => o.Stream).Returns(originalStream);
        features.Set(responseBodyMock.Object);

        var responseMock = new Mock<IHttpResponseFeature>();
        features.Set(responseMock.Object);

        var context = new DefaultHttpContext(features);

        Assert.Same(originalStream, context.Response.Body);
        Assert.Same(responseBodyMock.Object, context.Features.Get<IHttpResponseBodyFeature>());

        context.Response.Body = replacementStream;

        Assert.Same(replacementStream, context.Response.Body);
        Assert.NotSame(responseBodyMock.Object, context.Features.Get<IHttpResponseBodyFeature>());

        context.Response.Body = originalStream;

        Assert.Same(originalStream, context.Response.Body);
        Assert.Same(responseBodyMock.Object, context.Features.Get<IHttpResponseBodyFeature>());

        // The real issue was not that an OnCompleted registration existed, but that it would previously flush
        // the original response body in the OnCompleted callback after the response body was disposed.
        // However, since now there's no longer an OnCompleted registration at all, it's easier to verify that.
        // https://github.com/dotnet/aspnetcore/issues/25342
        responseMock.Verify(m => m.OnCompleted(It.IsAny<Func<object, Task>>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ResponseStart_CallsFeatureIfSet()
    {
        var features = new FeatureCollection();
        var mock = new Mock<IHttpResponseBodyFeature>();
        mock.Setup(o => o.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        features.Set(mock.Object);

        var responseMock = new Mock<IHttpResponseFeature>();
        responseMock.Setup(o => o.HasStarted).Returns(false);
        features.Set(responseMock.Object);

        var context = new DefaultHttpContext(features);
        await context.Response.StartAsync();

        mock.Verify(m => m.StartAsync(default), Times.Once());
    }

    [Fact]
    public async Task ResponseStart_CallsFeatureIfSetWithProvidedCancellationToken()
    {
        var features = new FeatureCollection();

        var mock = new Mock<IHttpResponseBodyFeature>();
        var ct = new CancellationToken();
        mock.Setup(o => o.StartAsync(It.Is<CancellationToken>((localCt) => localCt.Equals(ct)))).Returns(Task.CompletedTask);
        features.Set(mock.Object);

        var responseMock = new Mock<IHttpResponseFeature>();
        responseMock.Setup(o => o.HasStarted).Returns(false);
        features.Set(responseMock.Object);

        var context = new DefaultHttpContext(features);
        await context.Response.StartAsync(ct);

        mock.Verify(m => m.StartAsync(default), Times.Once());
    }

    [Fact]
    public async Task ResponseStart_DoesNotCallStartIfHasStartedIsTrue()
    {
        var features = new FeatureCollection();

        var startMock = new Mock<IHttpResponseBodyFeature>();
        startMock.Setup(o => o.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        features.Set(startMock.Object);

        var responseMock = new Mock<IHttpResponseFeature>();
        responseMock.Setup(o => o.HasStarted).Returns(true);
        features.Set(responseMock.Object);

        var context = new DefaultHttpContext(features);
        await context.Response.StartAsync();

        startMock.Verify(m => m.StartAsync(default), Times.Never());
    }

    [Fact]
    public async Task ResponseStart_CallsResponseBodyFlushIfNotSet()
    {
        var context = new DefaultHttpContext();
        var mock = new FlushAsyncCheckStream();
        context.Response.Body = mock;

        await context.Response.StartAsync(default);

        Assert.True(mock.IsCalled);
    }

    [Fact]
    public async Task RegisterForDisposeHandlesDisposeAsyncIfObjectImplementsIAsyncDisposable()
    {
        var features = new FeatureCollection();
        var response = new ResponseFeature();
        features.Set<IHttpResponseFeature>(response);

        var context = new DefaultHttpContext(features);
        var instance = new DisposableClass();
        context.Response.RegisterForDispose(instance);

        await response.ExecuteOnCompletedCallbacks();

        Assert.True(instance.DisposeAsyncCalled);
        Assert.False(instance.DisposeCalled);
    }

    public class ResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task>, object)> _callbacks = new();
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; }
        public Stream Body { get; set; }

        public bool HasStarted => false;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            _callbacks.Add((callback, state));
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public async Task ExecuteOnCompletedCallbacks()
        {
            foreach (var (callback, state) in _callbacks)
            {
                await callback(state);
            }
        }
    }

    public class DisposableClass : IDisposable, IAsyncDisposable
    {
        public bool DisposeCalled { get; set; }

        public bool DisposeAsyncCalled { get; set; }

        public void Dispose()
        {
            DisposeCalled = true;
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    private static HttpResponse CreateResponse(IHeaderDictionary headers)
    {
        var context = new DefaultHttpContext();
        context.Features.Get<IHttpResponseFeature>().Headers = headers;
        return context.Response;
    }

    private static HttpResponse GetResponseWithContentLength(string contentLength = null)
    {
        return GetResponseWithHeader("Content-Length", contentLength);
    }

    private static HttpResponse GetResponseWithContentType(string contentType = null)
    {
        return GetResponseWithHeader("Content-Type", contentType);
    }

    private static HttpResponse GetResponseWithHeader(string headerName, string headerValue)
    {
        var headers = new HeaderDictionary();
        if (headerValue != null)
        {
            headers.Add(headerName, headerValue);
        }

        return CreateResponse(headers);
    }

    private class FlushAsyncCheckStream : MemoryStream
    {
        public bool IsCalled { get; private set; }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            IsCalled = true;
            return base.FlushAsync(cancellationToken);
        }
    }
}
