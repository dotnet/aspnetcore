// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Tests;

public class NamedPipeConnectionTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [Fact]
    public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        await NamedPipeTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(
            NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint),
            connectionListener,
            Logger);

        Assert.Contains(LogMessages, m => m.Message.Contains("send loop completed gracefully"));
    }

    [Fact]
    public async Task InputReadAsync_ServerAborted_ThrowError()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        await clientStream.ConnectAsync().DefaultTimeout();
        await clientStream.WriteAsync(TestData).DefaultTimeout();
        
        var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();
        var readResult = await serverConnection.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverConnection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        serverConnection.Abort(new ConnectionAbortedException("Test reason"));

        var serverEx = await Assert.ThrowsAsync<ConnectionAbortedException>(() => serverConnection.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();
        Assert.Equal("Test reason", serverEx.Message);

        // Complete writing.
        await serverConnection.Transport.Output.CompleteAsync();
    }

    [Fact]
    public async Task InputReadAsync_ServerAbortedDuring_ThrowError()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        await clientStream.ConnectAsync().DefaultTimeout();
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();
        var readResult = await serverConnection.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverConnection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        var serverReadTask = serverConnection.Transport.Input.ReadAsync();
        Assert.False(serverReadTask.IsCompleted);

        serverConnection.Abort(new ConnectionAbortedException("Test reason"));

        var serverEx = await Assert.ThrowsAsync<ConnectionAbortedException>(() => serverReadTask.AsTask()).DefaultTimeout();
        Assert.Equal("Test reason", serverEx.Message);

        // Complete writing.
        await serverConnection.Transport.Output.CompleteAsync();
    }

    [Fact]
    public async Task OutputWriteAsync_ServerAborted_ThrowError()
    {
        // Arrange
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        await clientStream.ConnectAsync().DefaultTimeout();
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();
        var readResult = await serverConnection.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverConnection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        serverConnection.Abort(new ConnectionAbortedException("Test reason"));

        // Write after abort is ignored.
        await serverConnection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(new string('c', 1024 * 1024 * 10)));

        // Complete writing.
        await serverConnection.Transport.Output.CompleteAsync();
    }
}
