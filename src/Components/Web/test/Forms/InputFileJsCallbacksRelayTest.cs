// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputFileJsCallbacksRelayTest
{
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
        reference.Setup(stream => stream.OpenReadStreamAsync(bytes.Length, It.IsAny<CancellationToken>()))
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
}
