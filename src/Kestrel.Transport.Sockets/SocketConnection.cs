// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Protocols;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    internal sealed class SocketConnection : TransportConnection
    {
        private readonly Socket _socket;
        private readonly SocketTransport _transport;

        private IList<ArraySegment<byte>> _sendBufferList;
        private const int MinAllocBufferSize = 2048;

        internal SocketConnection(Socket socket, SocketTransport transport)
        {
            Debug.Assert(socket != null);
            Debug.Assert(transport != null);

            _socket = socket;
            _transport = transport;

            var localEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            var remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

            LocalAddress = localEndPoint.Address;
            LocalPort = localEndPoint.Port;

            RemoteAddress = remoteEndPoint.Address;
            RemotePort = remoteEndPoint.Port;
        }

        public async Task StartAsync(IConnectionHandler connectionHandler)
        {
            try
            {
                connectionHandler.OnConnection(this);

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
                    var buffer = Input.Alloc(MinAllocBufferSize);

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
                Input.Complete(error);
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
                    var result = await Output.ReadAsync();
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
                        Output.Advance(buffer.End);
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
                Output.Complete(error);
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

        public override PipeFactory PipeFactory => _transport.TransportFactory.PipeFactory;
        public override IScheduler InputWriterScheduler => InlineScheduler.Default;
        public override IScheduler OutputReaderScheduler => TaskRunScheduler.Default;
    }
}
