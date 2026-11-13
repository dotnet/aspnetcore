// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputFileJsCallbacksRelayTest
{
    private const int MaxMetadataBytes = 1024 * 1024;

    [Theory]
    [InlineData(MaxMetadataBytes - 1)]
    [InlineData(MaxMetadataBytes)]
    public async Task NotifyChangeFromStream_AcceptsMetadataWithinByteLimit(int metadataBytes)
    {
        const string prefix = "[{\"id\":1,\"name\":\"";
        const string suffix = "\"}]";
        var name = new string('m', metadataBytes - prefix.Length - suffix.Length);
        var bytes = Encoding.UTF8.GetBytes(prefix + name + suffix);
        using var runtime = new MetadataJSRuntime(bytes);
        var reference = runtime.CreateReference(bytes.Length);
        var callbacks = new TestCallbacks();
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        await relay.NotifyChangeFromStream(reference);

        Assert.Equal(metadataBytes, bytes.Length);
        Assert.Equal(name, Assert.Single(Assert.Single(callbacks.Notifications)).Name);
        Assert.Equal(1, runtime.StreamReadCount);
        Assert.True(runtime.ReferenceDisposed);
    }

    [Theory]
    [InlineData(MaxMetadataBytes + 1L)]
    [InlineData(long.MaxValue)]
    public async Task NotifyChangeFromStream_RejectsOversizedMetadataBeforeReading(long declaredLength)
    {
        using var runtime = new MetadataJSRuntime(Encoding.UTF8.GetBytes("[]"));
        var reference = runtime.CreateReference(declaredLength);
        var callbacks = new TestCallbacks();
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => relay.NotifyChangeFromStream(reference));

        Assert.Equal("maxAllowedSize", exception.ParamName);
        Assert.Equal(0, runtime.StreamReadCount);
        Assert.Empty(callbacks.Notifications);
        Assert.True(runtime.ReferenceDisposed);

        await relay.NotifyChange([]).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(Assert.Single(callbacks.Notifications));
    }

    [Fact]
    public async Task NotifyChangeFromStream_PreservesFileMetadata()
    {
        var reference = CreateReference("""
            [{"id":1,"name":"file-\u00e9.txt","lastModified":"2026-01-01T00:00:00.000Z","size":3,"contentType":"text/plain","relativePath":"directory/file.txt","blob":{}}]
            """);
        var callbacks = new TestCallbacks();
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        await relay.NotifyChangeFromStream(reference.Object);

        var file = Assert.Single(Assert.Single(callbacks.Notifications));
        Assert.Equal(1, file.Id);
        Assert.Equal("file-\u00e9.txt", file.Name);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), file.LastModified);
        Assert.Equal(3, file.Size);
        Assert.Equal("text/plain", file.ContentType);
        Assert.Equal("directory/file.txt", file.RelativePath);
        reference.Verify(stream => stream.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task NotifyChangeFromStream_PreservesSelectionAndCancelOrder()
    {
        var firstStream = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstReference = CreateReference("""[{"id":1}]""", firstStream.Task);
        var secondReference = CreateReference("""[{"id":2}]""");
        var callbacks = new TestCallbacks();
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        var firstNotification = relay.NotifyChangeFromStream(firstReference.Object);
        var cancelNotification = relay.NotifyChange([]);
        var secondNotification = relay.NotifyChangeFromStream(secondReference.Object);

        Assert.Empty(callbacks.Notifications);
        firstStream.SetResult(new MemoryStream(Encoding.UTF8.GetBytes("""[{"id":1}]""")));
        await Task.WhenAll(firstNotification, cancelNotification, secondNotification).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { 1, 0, 2 }, callbacks.Notifications.Select(files => files.Length == 0 ? 0 : files[0].Id));
    }

    [Fact]
    public async Task NotifyChangeFromStream_DoesNotWaitForPreviousApplicationCallback()
    {
        var applicationCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbacks = new TestCallbacks(files => files[0].Id == 1 ? applicationCallback.Task : Task.CompletedTask);
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        var firstNotification = relay.NotifyChangeFromStream(CreateReference("""[{"id":1}]""").Object);
        await relay.NotifyChangeFromStream(CreateReference("""[{"id":2}]""").Object).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(firstNotification.IsCompleted);
        Assert.Equal(new[] { 1, 2 }, callbacks.Notifications.Select(files => files[0].Id));
        applicationCallback.SetResult();
        await firstNotification;
    }

    [Fact]
    public async Task NotifyChangeFromStream_InvalidMetadataDoesNotBlockLaterNotifications()
    {
        var firstStream = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reference = CreateReference("[", firstStream.Task);
        var callbacks = new TestCallbacks();
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        var invalidNotification = relay.NotifyChangeFromStream(reference.Object);
        var cancelNotification = relay.NotifyChange([]);
        firstStream.SetResult(new MemoryStream(Encoding.UTF8.GetBytes("[")));

        await Assert.ThrowsAsync<JsonException>(() => invalidNotification);
        await cancelNotification.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(Assert.Single(callbacks.Notifications));
        reference.Verify(stream => stream.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task NotifyChangeFromStream_RejectsNullMetadata()
    {
        var callbacks = new TestCallbacks();
        var reference = CreateReference("null");
        using var relay = new InputFileJsCallbacksRelay(callbacks);

        await Assert.ThrowsAsync<JsonException>(() => relay.NotifyChangeFromStream(reference.Object));

        reference.Verify(stream => stream.DisposeAsync(), Times.Once);
        Assert.Empty(callbacks.Notifications);
    }

    [Fact]
    public async Task Dispose_CancelsPendingMetadataAndIsIdempotent()
    {
        var openStream = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reference = CreateReference("[]");
        reference.Setup(stream => stream.OpenReadStreamAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns<long, CancellationToken>((_, cancellationToken) => new ValueTask<Stream>(openStream.Task.WaitAsync(cancellationToken)));
        var callbacks = new TestCallbacks();
        var relay = new InputFileJsCallbacksRelay(callbacks);
        var notification = relay.NotifyChangeFromStream(reference.Object);

        relay.Dispose();
        relay.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);

        reference.Verify(stream => stream.DisposeAsync(), Times.Once);
        Assert.Empty(callbacks.Notifications);
    }

    private static Mock<IJSStreamReference> CreateReference(string json, Task<Stream>? openStream = null)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var reference = new Mock<IJSStreamReference>(MockBehavior.Strict);
        reference.SetupGet(stream => stream.Length).Returns(bytes.Length);
        reference.Setup(stream => stream.OpenReadStreamAsync(MaxMetadataBytes, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Stream>(openStream ?? Task.FromResult<Stream>(new MemoryStream(bytes))));
        reference.Setup(stream => stream.DisposeAsync()).Returns(ValueTask.CompletedTask);

        return reference;
    }

    private sealed class TestCallbacks(Func<BrowserFile[], Task>? onChange = null) : IInputFileJsCallbacks
    {
        public List<BrowserFile[]> Notifications { get; } = [];

        public Task NotifyChange(BrowserFile[] files)
        {
            Notifications.Add(files);

            return onChange?.Invoke(files) ?? Task.CompletedTask;
        }
    }

    private sealed class MetadataJSRuntime(byte[] bytes) : JSRuntime
    {
        public int StreamReadCount { get; private set; }

        public bool ReferenceDisposed { get; private set; }

        public IJSStreamReference CreateReference(long length)
            => JsonSerializer.Deserialize<IJSStreamReference>(
                $$"""{"__jsObjectId":1,"__jsStreamReferenceLength":{{length}}}""",
                JsonSerializerOptions)!;

        protected override Task<Stream> ReadJSDataAsStreamAsync(
            IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
        {
            StreamReadCount++;

            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        protected override void BeginInvokeJS(
            long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            Assert.Equal("DotNet.disposeJSObjectReferenceById", identifier);
            ReferenceDisposed = true;
            DotNetDispatcher.EndInvokeJS(this, $$"""[{{taskId}},true,null]""");
        }

        protected override void EndInvokeDotNet(
            DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotSupportedException();
    }
}
