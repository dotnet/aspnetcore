// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.InternalTesting;

#nullable enable

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class HttpResponseJsonExtensionsTests
{
    [Fact]
    public async Task WriteAsJsonAsyncGeneric_SimpleValue_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(1);

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        var data = body.ToArray();
        Assert.Collection(data, b => Assert.Equal((byte)'1', b));
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_NullValue_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync<Uri?>(value: null);

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);

        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal("null", data);
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_WithOptions_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntegerConverter());
        await context.Response.WriteAsJsonAsync(new int[] { 1, 2, 3 }, options);

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);

        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal("[false,true,false]", data);
    }

    private class IntegerConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value % 2 == 0);
        }
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_CustomStatusCode_StatusCodeUnchanged()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        context.Response.StatusCode = StatusCodes.Status418ImATeapot;
        await context.Response.WriteAsJsonAsync(1);

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_WithContentType_JsonResponseWithCustomContentType()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(1, options: null, contentType: "application/custom-type");

        // Assert
        Assert.Equal("application/custom-type", context.Response.ContentType);
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_WithCancellationToken_CancellationRaised()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new TestStream();

        var cts = new CancellationTokenSource();

        // Act
        var writeTask = context.Response.WriteAsJsonAsync(1, cts.Token);
        Assert.False(writeTask.IsCompleted);

        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await writeTask);
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_ObjectWithStrings_CamcelCaseAndNotEscaped()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var value = new TestObject
        {
            StringProperty = "激光這兩個字是甚麼意思"
        };

        // Act
        await context.Response.WriteAsJsonAsync(value);

        // Assert
        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal(@"{""stringProperty"":""激光這兩個字是甚麼意思""}", data);
    }

    [Fact]
    public async Task WriteAsJsonAsync_SimpleValue_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(1, typeof(int));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        var data = body.ToArray();
        Assert.Collection(data, b => Assert.Equal((byte)'1', b));
    }

    [Fact]
    public async Task WriteAsJsonAsync_NullValue_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(value: null, typeof(int?));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);

        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal("null", data);
    }

    [Fact]
    public async Task WriteAsJsonAsync_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await context.Response.WriteAsJsonAsync(value: null, type: null!));
    }

    [Fact]
    public async Task WriteAsJsonAsync_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await HttpResponseJsonExtensions.WriteAsJsonAsync(response: null!, value: null, typeof(int?)));
    }

    [Fact]
    public async Task WriteAsJsonAsync_ObjectWithStrings_CamcelCaseAndNotEscaped()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var value = new TestObject
        {
            StringProperty = "激光這兩個字是甚麼意思"
        };

        // Act
        await context.Response.WriteAsJsonAsync(value, typeof(TestObject));

        // Assert
        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal(@"{""stringProperty"":""激光這兩個字是甚麼意思""}", data);
    }

    [Fact]
    public async Task WriteAsJsonAsync_CustomStatusCode_StatusCodeUnchanged()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        context.Response.StatusCode = StatusCodes.Status418ImATeapot;
        await context.Response.WriteAsJsonAsync(1, typeof(int));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_AsyncEnumerable()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(AsyncEnumerable());

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Equal("[1,2]", Encoding.UTF8.GetString(body.ToArray()));

        async IAsyncEnumerable<int> AsyncEnumerable()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
        }
    }

    [Fact]
    public async Task WriteAsJsonAsync_AsyncEnumerable()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        await context.Response.WriteAsJsonAsync(AsyncEnumerable(), typeof(IAsyncEnumerable<int>));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        Assert.Equal("[1,2]", Encoding.UTF8.GetString(body.ToArray()));

        async IAsyncEnumerable<int> AsyncEnumerable()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
        }
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_AsyncEnumerable_ClosedConnecton()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        context.RequestAborted = cts.Token;
        var iterated = false;

        // Act
        await context.Response.WriteAsJsonAsync(AsyncEnumerable());

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // System.Text.Json might write the '[' before cancellation is observed
        Assert.InRange(body.ToArray().Length, 0, 1);
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            for (var i = 0; i < 100 && !cancellationToken.IsCancellationRequested; i++)
            {
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteAsJsonAsync_AsyncEnumerable_ClosedConnecton()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        context.RequestAborted = cts.Token;
        var iterated = false;

        // Act
        await context.Response.WriteAsJsonAsync(AsyncEnumerable(), typeof(IAsyncEnumerable<int>));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // System.Text.Json might write the '[' before cancellation is observed
        Assert.InRange(body.ToArray().Length, 0, 1);
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            for (var i = 0; i < 100 && !cancellationToken.IsCancellationRequested; i++)
            {
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteAsJsonAsync_AsyncEnumerable_UserPassedTokenThrows()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        context.RequestAborted = new CancellationToken(canceled: true);
        var cts = new CancellationTokenSource();
        var iterated = false;

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Response.WriteAsJsonAsync(AsyncEnumerable(), typeof(IAsyncEnumerable<int>), cts.Token));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // System.Text.Json might write the '[' before cancellation is observed
        Assert.InRange(body.ToArray().Length, 0, 1);
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            for (var i = 0; i < 100 && !cancellationToken.IsCancellationRequested; i++)
            {
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_AsyncEnumerable_UserPassedTokenThrows()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        context.RequestAborted = new CancellationToken(canceled: true);
        var cts = new CancellationTokenSource();
        var iterated = false;

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Response.WriteAsJsonAsync(AsyncEnumerable(), cts.Token));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // System.Text.Json might write the '[' before cancellation is observed
        Assert.InRange(body.ToArray().Length, 0, 1);
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            for (var i = 0; i < 100 && !cancellationToken.IsCancellationRequested; i++)
            {
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteAsJsonAsyncGeneric_WithJsonTypeInfo_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        var options = new JsonSerializerOptions();
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        await context.Response.WriteAsJsonAsync(new int[] { 1, 2, 3 }, (JsonTypeInfo<int[]>)options.GetTypeInfo(typeof(int[])));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);

        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal("[1,2,3]", data);
    }

    [Fact]
    public async Task WriteAsJsonAsync_NullValue_WithJsonTypeInfo_JsonResponse()
    {
        // Arrange
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        // Act
        var options = new JsonSerializerOptions();
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        await context.Response.WriteAsJsonAsync(value : null, options.GetTypeInfo(typeof(Uri)));

        // Assert
        Assert.Equal(ContentTypeConstants.JsonContentTypeWithCharset, context.Response.ContentType);

        var data = Encoding.UTF8.GetString(body.ToArray());
        Assert.Equal("null", data);
    }

    public class TestObject
    {
        public string? StringProperty { get; set; }
    }

    private class TestStream : Stream
    {
        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<int>();
            cancellationToken.Register(s => ((TaskCompletionSource<int>)s!).SetCanceled(), tcs);
            return new ValueTask<int>(tcs.Task);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();
            cancellationToken.Register(s => ((TaskCompletionSource)s!).SetCanceled(), tcs);
            return new ValueTask(tcs.Task);
        }
    }
}
