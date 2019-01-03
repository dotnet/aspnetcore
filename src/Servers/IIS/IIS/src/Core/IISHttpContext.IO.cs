// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal partial class IISHttpContext
    {
        /// <summary>
        /// Reads data from the Input pipe to the user.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            if (!_hasRequestReadingStarted)
            {
                InitializeRequestIO();
            }

            while (true)
            {
                var result = await _bodyInputPipe.Reader.ReadAsync(cancellationToken);
                var readableBuffer = result.Buffer;
                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        var actual = Math.Min(readableBuffer.Length, memory.Length);
                        readableBuffer = readableBuffer.Slice(0, actual);
                        readableBuffer.CopyTo(memory.Span);
                        return (int)actual;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _bodyInputPipe.Reader.AdvanceTo(readableBuffer.End, readableBuffer.End);
                }
            }
        }

        /// <summary>
        /// Writes data to the output pipe.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default(CancellationToken))
        {
            async Task WriteFirstAsync()
            {
                await InitializeResponse(flushHeaders: false);
                await _bodyOutput.WriteAsync(memory, cancellationToken);
            }

            return !HasResponseStarted ? WriteFirstAsync() : _bodyOutput.WriteAsync(memory, cancellationToken);
        }

        /// <summary>
        /// Flushes the data in the output pipe
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            async Task FlushFirstAsync()
            {
                await InitializeResponse(flushHeaders: true);
                await _bodyOutput.FlushAsync(cancellationToken);
            }

            return !HasResponseStarted ? FlushFirstAsync() : _bodyOutput.FlushAsync(cancellationToken);
        }

        private async Task ReadBody()
        {
            Exception error = null;
            try
            {
                while (true)
                {
                    var memory = _bodyInputPipe.Writer.GetMemory();

                    var read = await AsyncIO.ReadAsync(memory);

                    // End of body
                    if (read == 0)
                    {
                        break;
                    }

                    // Read was not canceled because of incoming write or IO stopping
                    if (read != -1)
                    {
                        _bodyInputPipe.Writer.Advance(read);
                    }

                    var result = await _bodyInputPipe.Writer.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }
            }
            catch (ConnectionResetException ex)
            {
                ConnectionReset();
                error = ex;
            }
            catch (Exception ex)
            {
                error = ex;
                Log.UnexpectedError(_logger, nameof(IISHttpContext), ex);
            }
            finally
            {
                _bodyInputPipe.Writer.Complete(error);
            }
        }

        private async Task WriteBody(bool flush = false)
        {
            Exception error = null;
            try
            {
                while (true)
                {
                    var result = await _bodyOutput.Reader.ReadAsync();

                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            await AsyncIO.WriteAsync(buffer);
                        }

                        // if request is done no need to flush, http.sys would do it for us
                        if (result.IsCompleted)
                        {
                            break;
                        }

                        flush = flush | result.IsCanceled;

                        if (flush)
                        {
                            await AsyncIO.FlushAsync(moreData: true);
                            flush = false;
                        }
                    }
                    finally
                    {
                        _bodyOutput.Reader.AdvanceTo(buffer.End);
                    }
                }
            }
            // We want to swallow IO exception and allow app to finish writing
            catch (ConnectionResetException)
            {
                ConnectionReset();
            }
            catch (Exception ex)
            {
                error = ex;
                Log.UnexpectedError(_logger, nameof(IISHttpContext), ex);
            }
            finally
            {
                _bodyOutput.Reader.Complete(error);
            }
        }

        private bool AbortIO()
        {
            if (Interlocked.CompareExchange(ref _requestAborted, 1, 0) != 0)
            {
                return false;
            }

            _bodyOutput.Dispose();

            var cts = _abortedCts;
            if (cts != null)
            {
                ThreadPool.QueueUserWorkItem(t =>
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Log.ApplicationError(_logger, ((IHttpConnectionFeature)this).ConnectionId, TraceIdentifier, ex);
                    }
                });
            }

            return true;
        }

        public void Abort(Exception reason)
        {
            _bodyOutput.Abort(reason);
            _streams.Abort(reason);
            NativeMethods.HttpCloseConnection(_pInProcessHandler);

            AbortIO();
        }

        internal void ConnectionReset()
        {
            if (AbortIO())
            {
                Log.ConnectionDisconnect(_logger, ((IHttpConnectionFeature)this).ConnectionId);
            }
        }
    }
}
