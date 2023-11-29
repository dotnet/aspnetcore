// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Tests;

[NamedPipesSupported]
public class NamedPipeConnectionTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52462")]
    [ConditionalFact]
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

    [ConditionalFact]
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

    [ConditionalFact]
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

    [ConditionalFact]
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

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Non-OS implementations use UDS with different transport behavior.")]
    public async Task CompleteTransport_BeforeClientHasReadLastBytes_DontLoseData()
    {
        var options = new NamedPipeTransportOptions();
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, options: options);

        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        var connectTask = clientStream.ConnectAsync();

        var connection = (NamedPipeConnection)await connectionListener.AcceptAsync();
        await connection.Transport.Output.WriteAsync(new byte[] { 41 });
        byte[] readBuffer = new byte[1];
        var readAmount = await clientStream.ReadAsync(readBuffer);
        Assert.Equal(1, readAmount);
        Assert.Equal(41, readBuffer[0]);
        await connection.Transport.Output.WriteAsync(new byte[] { 42 });

        connection.Transport.Output.Complete();

        // Wait for a short amount of time after completing transport before asserting the server is waiting for the client to finish reading.
        // Actual stream disconnection should happen after the last client read happens.
        await Task.Delay(100);
        Assert.False(connection._sendingTask.IsCompleted, "Sending isn't complete until the client reads data.");

        readAmount = await clientStream.ReadAsync(readBuffer);
        Assert.Equal(1, readAmount);
        Assert.Equal(42, readBuffer[0]);

        await connection._sendingTask.DefaultTimeout();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Non-OS implementations use UDS with different transport behavior.")]
    public async Task DisposeAsync_BeforeClientHasReadLastBytes_DontLoseData()
    {
        var options = new NamedPipeTransportOptions();
        await using var connectionListener = await NamedPipeTestHelpers.CreateConnectionListenerFactory(LoggerFactory, options: options);

        var clientStream = NamedPipeTestHelpers.CreateClientStream(connectionListener.EndPoint);
        var connectTask = clientStream.ConnectAsync();

        var connection = (NamedPipeConnection)await connectionListener.AcceptAsync();
        await connection.Transport.Output.WriteAsync(new byte[] { 41 });
        byte[] readBuffer = new byte[1];
        var readAmount = await clientStream.ReadAsync(readBuffer);
        Assert.Equal(1, readAmount);
        Assert.Equal(41, readBuffer[0]);
        await connection.Transport.Output.WriteAsync(new byte[] { 42 });

        var disposeTask = connection.DisposeAsync();

        // Wait for a short amount of time after completing transport before asserting the server is waiting for the client to finish reading.
        // Actual stream disconnection should happen after the last client read happens.
        await Task.Delay(100);
        Assert.False(connection._sendingTask.IsCompleted, "Sending isn't complete until the client reads data.");
        Assert.False(disposeTask.IsCompleted, "Sending isn't complete until the client reads data.");

        readAmount = await clientStream.ReadAsync(readBuffer);
        Assert.Equal(1, readAmount);
        Assert.Equal(42, readBuffer[0]);

        await connection._sendingTask.DefaultTimeout();
        await disposeTask.DefaultTimeout();
    }
}
