// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class FrameConnection : IConnectionContext, ITimeoutControl
    {
        private readonly FrameConnectionContext _context;
        private readonly Frame _frame;
        private readonly List<IConnectionAdapter> _connectionAdapters;
        private readonly TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;

        private Task _lifetimeTask;
        private Stream _filteredStream;

        public FrameConnection(FrameConnectionContext context)
        {
            _context = context;
            _frame = context.Frame;
            _connectionAdapters = context.ConnectionAdapters;
        }

        public string ConnectionId => _context.ConnectionId;
        public IPipeWriter Input => _context.Input.Writer;
        public IPipeReader Output => _context.Output.Reader;

        private PipeFactory PipeFactory => _context.PipeFactory;

        // Internal for testing
        internal PipeOptions AdaptedInputPipeOptions => new PipeOptions
        {
            ReaderScheduler = _context.ServiceContext.ThreadPool,
            WriterScheduler = InlineScheduler.Default,
            MaximumSizeHigh = _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            MaximumSizeLow = _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0
        };

        internal PipeOptions AdaptedOutputPipeOptions => new PipeOptions
        {
            ReaderScheduler = InlineScheduler.Default,
            WriterScheduler = InlineScheduler.Default,
            MaximumSizeHigh = _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            MaximumSizeLow = _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0
        };

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public void StartRequestProcessing()
        {
            _lifetimeTask = ProcessRequestsAsync();
        }

        private async Task ProcessRequestsAsync()
        {
            RawStream rawStream = null;

            try
            {
                Task adaptedPipelineTask = Task.CompletedTask;

                if (_connectionAdapters.Count == 0)
                {
                    _frame.Input = _context.Input.Reader;
                    _frame.Output = _context.OutputProducer;
                }
                else
                {
                    rawStream = new RawStream(_context.Input.Reader, _context.OutputProducer);

                    try
                    {
                        var adaptedPipeline = await ApplyConnectionAdaptersAsync(rawStream);

                        _frame.Input = adaptedPipeline.Input.Reader;
                        _frame.Output = adaptedPipeline.Output;

                        adaptedPipelineTask = adaptedPipeline.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");

                        // Since Frame.ProcessRequestsAsync() isn't called, we have to close the socket here.
                        _context.OutputProducer.Dispose();

                        await _socketClosedTcs.Task;
                        return;
                    }

                }

                _frame.TimeoutControl = this;
                _lastTimestamp = _context.ServiceContext.SystemClock.UtcNow.Ticks;
                _context.ServiceContext.ConnectionManager.AddConnection(_context.FrameConnectionId, this);

                await _frame.ProcessRequestsAsync();
                await adaptedPipelineTask;
                await _socketClosedTcs.Task;
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Unexpected exception in {nameof(FrameConnection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
                _context.ServiceContext.ConnectionManager.RemoveConnection(_context.FrameConnectionId);
                rawStream?.Dispose();
                DisposeAdaptedConnections();
            }
        }

        public void OnConnectionClosed(Exception ex)
        {
            // Abort the connection (if it isn't already aborted)
            _frame.Abort(ex);

            Log.ConnectionStop(ConnectionId);
            KestrelEventSource.Log.ConnectionStop(this);
            _socketClosedTcs.TrySetResult(null);
        }

        public Task StopAsync()
        {
            _frame.Stop();
            return _lifetimeTask;
        }

        public void Abort(Exception ex)
        {
            _frame.Abort(ex);
        }

        public Task AbortAsync(Exception ex)
        {
            _frame.Abort(ex);
            return _lifetimeTask;
        }

        public void Timeout()
        {
            _frame.SetBadRequestState(RequestRejectionReason.RequestTimeout);
        }

        private async Task<AdaptedPipeline> ApplyConnectionAdaptersAsync(RawStream rawStream)
        {
            var adapterContext = new ConnectionAdapterContext(rawStream);
            var adaptedConnections = new IAdaptedConnection[_connectionAdapters.Count];

            for (var i = 0; i < _connectionAdapters.Count; i++)
            {
                var adaptedConnection = await _connectionAdapters[i].OnConnectionAsync(adapterContext);
                adaptedConnections[i] = adaptedConnection;
                adapterContext = new ConnectionAdapterContext(adaptedConnection.ConnectionStream);
            }

            _filteredStream = adapterContext.ConnectionStream;
            _frame.AdaptedConnections = adaptedConnections;

            return new AdaptedPipeline(_filteredStream,
                PipeFactory.Create(AdaptedInputPipeOptions),
                PipeFactory.Create(AdaptedOutputPipeOptions),
                Log);
        }

        private void DisposeAdaptedConnections()
        {
            var adaptedConnections = _frame.AdaptedConnections;
            if (adaptedConnections != null)
            {
                for (int i = adaptedConnections.Length - 1; i >= 0; i--)
                {
                    adaptedConnections[i].Dispose();
                }
            }
        }

        public void Tick(DateTimeOffset now)
        {
            var timestamp = now.Ticks;

            // TODO: Use PlatformApis.VolatileRead equivalent again
            if (timestamp > Interlocked.Read(ref _timeoutTimestamp))
            {
                CancelTimeout();

                if (_timeoutAction == TimeoutAction.SendTimeoutResponse)
                {
                    Timeout();
                }

                _frame.Stop();
            }

            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        public void SetTimeout(long ticks, TimeoutAction timeoutAction)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported");

            AssignTimeout(ticks, timeoutAction);
        }

        public void ResetTimeout(long ticks, TimeoutAction timeoutAction)
        {
            AssignTimeout(ticks, timeoutAction);
        }

        public void CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long ticks, TimeoutAction timeoutAction)
        {
            _timeoutAction = timeoutAction;

            // Add Heartbeat.Interval since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, _lastTimestamp + ticks + Heartbeat.Interval.Ticks);
        }
    }
}
