// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.ServerSentEvents;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ServerSentEventsResultTests
{
    [Fact]
    public async Task ExecuteAsync_SetsContentTypeAndHeaders()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = AsyncEnumerable.Empty<string>();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("text/event-stream", httpContext.Response.ContentType);
        Assert.Equal("no-cache,no-store", httpContext.Response.Headers.CacheControl);
        Assert.Equal("no-cache", httpContext.Response.Headers.Pragma);
        Assert.Equal("identity", httpContext.Response.Headers.ContentEncoding);
    }

    [Fact]
    public async Task ExecuteAsync_WritesStringEventsToResponse()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = new[] { "event1\"with\"quotes", "event2" }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("data: event1\"with\"quotes\n\n", responseBody);
        Assert.Contains("data: event2\n\n", responseBody);

        // Verify strings are not JSON serialized
        Assert.DoesNotContain("data: \"event1", responseBody);
        Assert.DoesNotContain("data: \"event2", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WritesStringsEventsWithEventType()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = new[] { "event1", "event2" }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events, "test-event");

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("event: test-event\ndata: event1\n\n", responseBody);
        Assert.Contains("event: test-event\ndata: event2\n\n", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithSseItems_WritesStringEventsDirectly()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = new[] { new SseItem<string>("event1", "custom-event") }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("event: custom-event\n", responseBody);
        Assert.Contains("data: event1\n\n", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var events = AsyncEnumerable.Empty<string>();
        var result = TypedResults.ServerSentEvents(events);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNullData()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = new string[] { null }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("data: \n\n", responseBody);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        ServerSentEventsResult<string> MyApi() { throw new NotImplementedException(); }
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<ServerSentEventsResult<string>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status200OK, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(SseItem<string>), producesResponseTypeMetadata.Type);
        Assert.Collection(producesResponseTypeMetadata.ContentTypes,
            contentType => Assert.Equal("text/event-stream", contentType));
    }

    [Fact]
    public async Task ExecuteAsync_WithObjectData_SerializesAsJson()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var testObject = new TestObject { Name = "Test", Value = 42 };
        var events = new[] { testObject }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains(@"data: {""name"":""Test"",""value"":42}", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithSsItems_SerializesDataAsJson()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var testObject = new TestObject { Name = "Test", Value = 42 };
        var events = new[] { new SseItem<TestObject>(testObject) }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains(@"data: {""name"":""Test"",""value"":42}", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomJsonOptions_UsesConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
        });
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            RequestServices = services.BuildServiceProvider()
        };

        var testObject = new TestObject { Name = "Test", Value = 42 };
        var events = new[] { testObject }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains(@"data: {""Name"":""Test"",""Value"":42}", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithPolymorphicType_SerializesCorrectly()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var baseClass = new DerivedTestObject { Name = "Test", Value = 42, Extra = "Additional" };
        var events = new TestObject[] { baseClass }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains(@"data: {""extra"":""Additional"",""name"":""Test"",""value"":42}", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_ObservesCancellationViaRequestAborted()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var httpContext = GetHttpContext();
        httpContext.RequestAborted = cts.Token;
        var firstEventReceived = new TaskCompletionSource();
        var secondEventAttempted = new TaskCompletionSource();
        var cancellationObserved = new TaskCompletionSource();

        var events = GetEvents(cts.Token);
        var result = TypedResults.ServerSentEvents(events);

        // Act & Assert
        var executeTask = result.ExecuteAsync(httpContext);

        // Wait for first event to be processed then cancel the request and wait
        // to observe the cancellation
        await firstEventReceived.Task;
        cts.Cancel();
        await secondEventAttempted.Task;

        // Verify the execution was cancelled and only the first event was written
        await Assert.ThrowsAsync<TaskCanceledException>(() => executeTask);
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("data: event1\n\n", responseBody);
        Assert.DoesNotContain("data: event2\n\n", responseBody);

        async IAsyncEnumerable<string> GetEvents([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                yield return "event1";
                firstEventReceived.SetResult();
                cancellationToken.Register(cancellationObserved.SetResult);
                await cancellationObserved.Task;
                yield return "event2";
            }
            finally
            {
                secondEventAttempted.SetResult();
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_DisablesBuffering()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = AsyncEnumerable.Empty<string>();
        var result = TypedResults.ServerSentEvents(events);
        var bufferingDisabled = false;

        var mockBufferingFeature = new MockHttpResponseBodyFeature(
            onDisableBuffering: () => bufferingDisabled = true);

        httpContext.Features.Set<IHttpResponseBodyFeature>(mockBufferingFeature);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.True(bufferingDisabled);
    }

    [Fact]
    public async Task ExecuteAsync_WithByteArrayData_WritesDataDirectly()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var bytes = "event1"u8.ToArray();
        var events = new[] { new SseItem<byte[]>(bytes) }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("data: event1\n\n", responseBody);

        // Assert that string is not JSON serialized
        Assert.DoesNotContain("data: \"event1", responseBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithByteArrayData_HandlesNullData()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var events = new[] { new SseItem<byte[]>(null) }.ToAsyncEnumerable();
        var result = TypedResults.ServerSentEvents(events);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        var responseBody = Encoding.UTF8.GetString(((MemoryStream)httpContext.Response.Body).ToArray());
        Assert.Contains("data: \n\n", responseBody);
    }

    private class MockHttpResponseBodyFeature(Action onDisableBuffering) : IHttpResponseBodyFeature
    {
        public Stream Stream => new MemoryStream();
        public PipeWriter Writer => throw new NotImplementedException();
        public Task CompleteAsync() => throw new NotImplementedException();
        public void DisableBuffering() => onDisableBuffering();
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task StartAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private static DefaultHttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private class TestObject
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    private class DerivedTestObject : TestObject
    {
        public string Extra { get; set; }
    }
}
