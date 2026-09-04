// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsConnectionTelemetryTests : LoggedTest
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ReceiveEof_LogsConnectionReadFin()
    {
        using var pump = new TestTlsEventPump();
        var connectionState = new TelemetryConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            TlsOperationStatus.Closed);
        var connection = CreateConnection(connectionState, pump);

        connection.Start();

        var readResult = await connection.Transport.Input.ReadAsync().AsTask().WaitAsync(Timeout);
        connection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        Assert.True(readResult.IsCompleted);
        Assert.Contains(TestSink.Writes, write => write.EventId.Name == "ConnectionReadFin");

        await connection.DisposeAsync();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task CompletedOutput_LogsConnectionWriteFin()
    {
        using var pump = new TestTlsEventPump();
        var connectionState = new TelemetryConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            TlsOperationStatus.NeedMoreData);
        var connection = CreateConnection(connectionState, pump);

        connection.Start();
        await connection.Transport.Output.CompleteAsync();
        await WaitForLogAsync("ConnectionWriteFin");

        await connection.DisposeAsync();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task Abort_LogsConnectionWriteRst()
    {
        using var pump = new TestTlsEventPump();
        var connectionState = new TelemetryConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            TlsOperationStatus.NeedMoreData);
        var connection = CreateConnection(connectionState, pump);

        connection.Abort(new ConnectionAbortedException("Test abort."));

        Assert.Contains(TestSink.Writes, write => write.EventId.Name == "ConnectionWriteRst");

        await connection.DisposeAsync();
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    [InlineData(false, "ConnectionError")]
    [InlineData(true, "ConnectionReset")]
    public async Task FatalError_LogsExpectedConnectionEvent(bool reset, string expectedEvent)
    {
        using var pump = new TestTlsEventPump();
        var connectionState = new TelemetryConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            TlsOperationStatus.NeedMoreData);
        var connection = CreateConnection(connectionState, pump);
        Exception error = reset
            ? new ConnectionResetException("Test reset.")
            : new IOException("Test error.");

        connection.Start();
        connectionState.OnError(error);
        await connection.DisposeAsync();

        Assert.Contains(TestSink.Writes, write => write.EventId.Name == expectedEvent);
    }

    private DirectTlsConnection CreateConnection(
        ConnectionIoState connectionState,
        TlsEventPump pump)
    {
        return new DirectTlsConnection(
            connectionState,
            pump,
            localEndPoint: null,
            remoteEndPoint: null,
            MemoryPool<byte>.Shared,
            maxReadBufferSize: 0,
            maxWriteBufferSize: 0,
            LoggerFactory.CreateLogger<DirectTlsConnection>());
    }

    private async Task WaitForLogAsync(string eventName)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!TestSink.Writes.Any(write => write.EventId.Name == eventName))
        {
            await Task.Delay(10, cts.Token);
        }
    }

    private sealed class TelemetryConnectionIoState : ConnectionIoState
    {
        private readonly TlsOperationStatus _readStatus;

        public TelemetryConnectionIoState(
            ILogger logger,
            TlsOperationStatus readStatus)
            : base(fd: 7, session: null!, logger)
        {
            _readStatus = readStatus;
            SetHandshakeComplete();
        }

        internal override TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
        {
            bytesRead = 0;
            return _readStatus;
        }

        internal override void ApplyEvents(uint events)
        {
        }

        internal override void ShutdownSession()
        {
        }

        internal override void DisposeSession()
        {
        }
    }

    private sealed class TestTlsEventPump()
        : TlsEventPump(NullLogger<TlsEventPump>.Instance, id: 0, System.Threading.Timeout.InfiniteTimeSpan)
    {
        internal override void DeregisterFromEpoll(int fd)
        {
        }
    }
}
