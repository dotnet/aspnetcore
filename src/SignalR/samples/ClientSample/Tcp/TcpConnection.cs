// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace ClientSample;

public class TcpConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
{
    private readonly Socket _socket;
    private volatile bool _aborted;
    private readonly EndPoint _endPoint;
    private IDuplexPipe _application;
    private readonly SocketSender _sender;
    private readonly SocketReceiver _receiver;

    public TcpConnection(EndPoint endPoint)
    {
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _endPoint = endPoint;

        _sender = new SocketSender(_socket, PipeScheduler.ThreadPool);
        _receiver = new SocketReceiver(_socket, PipeScheduler.ThreadPool);

        // Add IConnectionInherentKeepAliveFeature to the tcp connection impl since Kestrel doesn't implement
        // the IConnectionHeartbeatFeature
        Features.Set<IConnectionInherentKeepAliveFeature>(this);
    }

    public override IDuplexPipe Transport { get; set; }

    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override string ConnectionId { get; set; } = Guid.NewGuid().ToString();
    public override IDictionary<object, object> Items { get; set; } = new ConnectionItems();

    // We claim to have inherent keep-alive so the client doesn't kill the connection when it hasn't seen ping frames.
    public bool HasInherentKeepAlive { get; } = true;

    public override ValueTask DisposeAsync()
    {
        Transport?.Output.Complete();
        Transport?.Input.Complete();

        _socket?.Dispose();

        return default;
    }

    public async ValueTask<ConnectionContext> StartAsync()
    {
        await _socket.ConnectAsync(_endPoint);

        var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

        Transport = pair.Transport;
        _application = pair.Application;

        _ = ExecuteAsync();

        return this;
    }

    private async Task ExecuteAsync()
    {
        Exception sendError = null;
        try
        {
            // Spawn send and receive logic
            var receiveTask = DoReceive();
            var sendTask = DoSend();

            // If the sending task completes then close the receive
            // We don't need to do this in the other direction because the kestrel
            // will trigger the output closing once the input is complete.
            if (await Task.WhenAny(receiveTask, sendTask) == sendTask)
            {
                // Tell the reader it's being aborted
                _socket.Dispose();
            }

            // Now wait for both to complete
            await receiveTask;
            sendError = await sendTask;

            // Dispose the socket(should noop if already called)
            _socket.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected exception in {nameof(TcpConnection)}.{nameof(StartAsync)}: " + ex);
        }
        finally
        {
            // Complete the output after disposing the socket
            _application.Input.Complete(sendError);
        }
    }
    private async Task DoReceive()
    {
        Exception error = null;

        try
        {
            await ProcessReceives();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
        {
            error = new ConnectionResetException(ex.Message, ex);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted ||
                                         ex.SocketErrorCode == SocketError.ConnectionAborted ||
                                         ex.SocketErrorCode == SocketError.Interrupted ||
                                         ex.SocketErrorCode == SocketError.InvalidArgument)
        {
            if (!_aborted)
            {
                // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
                error = new ConnectionAbortedException();
            }
        }
        catch (ObjectDisposedException)
        {
            if (!_aborted)
            {
                error = new ConnectionAbortedException();
            }
        }
        catch (IOException ex)
        {
            error = ex;
        }
        catch (Exception ex)
        {
            error = new IOException(ex.Message, ex);
        }
        finally
        {
            if (_aborted)
            {
                error = error ?? new ConnectionAbortedException();
            }

            _application.Output.Complete(error);
        }
    }

    private async Task ProcessReceives()
    {
        while (true)
        {
            // Ensure we have some reasonable amount of buffer space
            var buffer = _application.Output.GetMemory();

            var bytesReceived = await _receiver.ReceiveAsync(buffer);

            if (bytesReceived == 0)
            {
                // FIN
                break;
            }

            _application.Output.Advance(bytesReceived);

            var result = await _application.Output.FlushAsync();
            if (result.IsCompleted)
            {
                // Pipe consumer is shut down, do we stop writing
                break;
            }
        }
    }

    private async Task<Exception> DoSend()
    {
        Exception error = null;

        try
        {
            await ProcessSends();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
        {
            error = null;
        }
        catch (ObjectDisposedException)
        {
            error = null;
        }
        catch (IOException ex)
        {
            error = ex;
        }
        catch (Exception ex)
        {
            error = new IOException(ex.Message, ex);
        }
        finally
        {
            _aborted = true;
            _socket.Shutdown(SocketShutdown.Both);
        }

        return error;
    }

    private async Task ProcessSends()
    {
        while (true)
        {
            // Wait for data to write from the pipe producer
            var result = await _application.Input.ReadAsync();
            var buffer = result.Buffer;

            if (result.IsCanceled)
            {
                break;
            }

            var end = buffer.End;
            var isCompleted = result.IsCompleted;
            if (!buffer.IsEmpty)
            {
                await _sender.SendAsync(buffer);
            }

            _application.Input.AdvanceTo(end);

            if (isCompleted)
            {
                break;
            }
        }
    }
}
