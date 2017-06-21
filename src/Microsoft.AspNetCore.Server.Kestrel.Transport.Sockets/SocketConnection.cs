// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    internal sealed class SocketConnection : IConnectionInformation
    {
        private readonly Socket _socket;
        private readonly SocketTransport _transport;
        private readonly IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;
        private IConnectionContext _connectionContext;
        private IPipeWriter _input;
        private IPipeReader _output;
        private IList<ArraySegment<byte>> _sendBufferList;
        private const int MinAllocBufferSize = 2048;

        internal SocketConnection(Socket socket, SocketTransport transport)
        {
            Debug.Assert(socket != null);
            Debug.Assert(transport != null);

            _socket = socket;
            _transport = transport;

            _localEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            _remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
        }

        public async void Start(IConnectionHandler connectionHandler)
        {
            try
            {
                _connectionContext = connectionHandler.OnConnection(this);

                _input = _connectionContext.Input;
                _output = _connectionContext.Output;

                // Spawn send and receive logic
                Task receiveTask = DoReceive();
                Task sendTask = DoSend();

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
                await sendTask;

                // Dispose the socket(should noop if already called)
                _socket.Dispose();
            }
            catch (Exception)
            {
                // TODO: Log
            }
        }

        private async Task DoReceive()
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    // Ensure we have some reasonable amount of buffer space
                    var buffer = _input.Alloc(MinAllocBufferSize);

                    try
                    {
                        var bytesReceived = await _socket.ReceiveAsync(GetArraySegment(buffer.Buffer), SocketFlags.None);

                        if (bytesReceived == 0)
                        {
                            // FIN
                            break;
                        }

                        buffer.Advance(bytesReceived);
                    }
                    finally
                    {
                        buffer.Commit();
                    }

                    var result = await buffer.FlushAsync();
                    if (result.IsCompleted)
                    {
                        // Pipe consumer is shut down, do we stop writing
                        break;
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                error = new ConnectionResetException(ex.Message, ex);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                error = new ConnectionAbortedException();
            }
            catch (ObjectDisposedException)
            {
                error = new ConnectionAbortedException();
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
                _connectionContext.Abort(error);
                _input.Complete(error);
            }
        }

        private void SetupSendBuffers(ReadableBuffer buffer)
        {
            Debug.Assert(!buffer.IsEmpty);
            Debug.Assert(!buffer.IsSingleSpan);

            if (_sendBufferList == null)
            {
                _sendBufferList = new List<ArraySegment<byte>>();
            }

            // We should always clear the list after the send
            Debug.Assert(_sendBufferList.Count == 0);

            foreach (var b in buffer)
            {
                _sendBufferList.Add(GetArraySegment(b));
            }
        }

        private async Task DoSend()
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    // Wait for data to write from the pipe producer
                    var result = await _output.ReadAsync();
                    var buffer = result.Buffer;

                    if (result.IsCancelled)
                    {
                        break;
                    }

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            if (buffer.IsSingleSpan)
                            {
                                await _socket.SendAsync(GetArraySegment(buffer.First), SocketFlags.None);
                            }
                            else
                            {
                                SetupSendBuffers(buffer);

                                try
                                {
                                    await _socket.SendAsync(_sendBufferList, SocketFlags.None);
                                }
                                finally
                                {
                                    _sendBufferList.Clear();
                                }
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _output.Advance(buffer.End);
                    }
                }

                _socket.Shutdown(SocketShutdown.Send);
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
                _connectionContext.OnConnectionClosed(error);
                _output.Complete(error);
            }
        }

        private static ArraySegment<byte> GetArraySegment(Buffer<byte> buffer)
        {
            if (!buffer.TryGetArray(out var segment))
            {
                throw new InvalidOperationException();
            }

            return segment;
        }

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;

        public IPEndPoint LocalEndPoint => _localEndPoint;

        public PipeFactory PipeFactory => _transport.TransportFactory.PipeFactory;

        public IScheduler InputWriterScheduler => InlineScheduler.Default;

        public IScheduler OutputReaderScheduler => TaskRunScheduler.Default;
    }
}
