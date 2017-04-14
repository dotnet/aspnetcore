// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;

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

        private const int MinAllocBufferSize = 2048;        // from libuv transport

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

                // Wait for eiher of them to complete (note they won't throw exceptions)
                await Task.WhenAny(receiveTask, sendTask);

                // Shut the socket down and wait for both sides to end
                _socket.Shutdown(SocketShutdown.Both);

                // Now wait for both to complete
                await receiveTask;
                await sendTask;

                // Dispose the socket
                _socket.Dispose();
            }
            catch (Exception)
            {
                // TODO: Log
            }
            finally
            {
                // Mark the connection as closed after disposal
                _connectionContext.OnConnectionClosed();
            }
        }

        private async Task DoReceive()
        {
            try
            {
                while (true)
                {
                    // Ensure we have some reasonable amount of buffer space
                    var buffer = _input.Alloc(MinAllocBufferSize);

                    int bytesReceived;

                    try
                    {
                        bytesReceived = await _socket.ReceiveAsync(GetArraySegment(buffer.Buffer), SocketFlags.None);
                    }
                    catch (Exception)
                    {
                        buffer.Commit();
                        throw;
                    }

                    if (bytesReceived == 0)
                    {
                        buffer.Commit();

                        // We receive a FIN so throw an exception so that we cancel the input
                        // with an error
                        throw new TaskCanceledException("The request was aborted");
                    }

                    // Record what data we filled into the buffer and push to pipe
                    buffer.Advance(bytesReceived);

                    var result = await buffer.FlushAsync();
                    if (result.IsCompleted)
                    {
                        // Pipe consumer is shut down, do we stop writing
                        _socket.Shutdown(SocketShutdown.Receive);
                        break;
                    }
                }

                _input.Complete();
            }
            catch (Exception ex)
            {
                _connectionContext.Abort(ex);
                _input.Complete(ex);
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
            try
            {
                while (true)
                {
                    // Wait for data to write from the pipe producer
                    var result = await _output.ReadAsync();
                    var buffer = result.Buffer;

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

                        if (result.IsCancelled)
                        {
                            // Send a FIN
                            _socket.Shutdown(SocketShutdown.Send);
                            break;
                        }

                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _output.Advance(buffer.End);
                    }
                }

                // We're done reading
                _output.Complete();
            }
            catch (Exception ex)
            {
                _output.Complete(ex);
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

        public bool RequiresDispatch => _transport.TransportFactory.ForceDispatch;

        public IScheduler InputWriterScheduler => InlineScheduler.Default;

        public IScheduler OutputReaderScheduler => InlineScheduler.Default;
    }
}
