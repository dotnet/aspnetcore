// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class FrameConnection : IConnectionContext, ITimeoutControl
    {
        private readonly FrameConnectionContext _context;
        private readonly Frame _frame;
        private readonly List<IConnectionAdapter> _connectionAdapters;
        private readonly TaskCompletionSource<object> _frameStartedTcs = new TaskCompletionSource<object>();

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;

        private AdaptedPipeline _adaptedPipeline;
        private Stream _filteredStream;
        private Task _adaptedPipelineTask = TaskCache.CompletedTask;

        public FrameConnection(FrameConnectionContext context)
        {
            _context = context;
            _frame = context.Frame;
            _connectionAdapters = context.ConnectionAdapters;
            context.ServiceContext.ConnectionManager.AddConnection(context.FrameConnectionId, this);
        }

        public string ConnectionId => _context.ConnectionId;
        public IPipeWriter Input => _context.Input.Writer;
        public IPipeReader Output => _context.Output.Reader;

        private PipeFactory PipeFactory => _context.PipeFactory;

        // Internal for testing
        internal PipeOptions AdaptedPipeOptions => new PipeOptions
        {
            ReaderScheduler = InlineScheduler.Default,
            WriterScheduler = InlineScheduler.Default,
            MaximumSizeHigh = _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            MaximumSizeLow = _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0
        };

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public void StartRequestProcessing()
        {
            _frame.Input = _context.Input.Reader;
            _frame.Output = _context.OutputProducer;
            _frame.TimeoutControl = this;

            if (_connectionAdapters.Count == 0)
            {
                StartFrame();
            }
            else
            {
                // Ensure that IConnectionAdapter.OnConnectionAsync does not run on the transport thread.
                _context.ServiceContext.ThreadPool.UnsafeRun(state =>
                {
                    // ApplyConnectionAdaptersAsync should never throw. If it succeeds, it will call _frame.Start().
                    // Otherwise, it will close the connection.
                    var ignore = ((FrameConnection)state).ApplyConnectionAdaptersAsync();
                }, this);
            }
        }

        public void OnConnectionClosed()
        {
            _context.ServiceContext.ConnectionManager.RemoveConnection(_context.FrameConnectionId);
            Log.ConnectionStop(ConnectionId);
            KestrelEventSource.Log.ConnectionStop(this);
        }

        public async Task StopAsync()
        {
            await _frameStartedTcs.Task;
            await _frame.StopAsync();
            await _adaptedPipelineTask;
        }

        public void Abort(Exception ex)
        {
            _frame.Abort(ex);
        }

        public void Timeout()
        {
            _frame.SetBadRequestState(RequestRejectionReason.RequestTimeout);
        }

        private async Task ApplyConnectionAdaptersAsync()
        {
            try
            {
                var rawSocketOutput = _frame.Output;
                var rawStream = new RawStream(_frame.Input, rawSocketOutput);
                var adapterContext = new ConnectionAdapterContext(rawStream);
                var adaptedConnections = new IAdaptedConnection[_connectionAdapters.Count];

                for (var i = 0; i < _connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await _connectionAdapters[i].OnConnectionAsync(adapterContext);
                    adaptedConnections[i] = adaptedConnection;
                    adapterContext = new ConnectionAdapterContext(adaptedConnection.ConnectionStream);
                }

                if (adapterContext.ConnectionStream != rawStream)
                {
                    _filteredStream = adapterContext.ConnectionStream;
                    _adaptedPipeline = new AdaptedPipeline(
                        adapterContext.ConnectionStream,
                        PipeFactory.Create(AdaptedPipeOptions),
                        PipeFactory.Create(AdaptedPipeOptions));

                    _frame.Input = _adaptedPipeline.Input.Reader;
                    _frame.Output = _adaptedPipeline.Output;

                    _adaptedPipelineTask = RunAdaptedPipeline();
                }

                _frame.AdaptedConnections = adaptedConnections;
                StartFrame();
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");
                _frameStartedTcs.SetResult(null);
                CloseRawPipes();
            }
        }

        private async Task RunAdaptedPipeline()
        {
            try
            {
                await _adaptedPipeline.RunAsync();
            }
            catch (Exception ex)
            {
                // adaptedPipeline.RunAsync() shouldn't throw.
                Log.LogError(0, ex, $"{nameof(FrameConnection)}.{nameof(ApplyConnectionAdaptersAsync)}");
            }
            finally
            {
                CloseRawPipes();
            }
        }

        private void CloseRawPipes()
        {
            _filteredStream?.Dispose();
            _context.OutputProducer.Dispose();
            _context.Input.Reader.Complete();
        }

        private void StartFrame()
        {
            _lastTimestamp = _context.ServiceContext.SystemClock.UtcNow.Ticks;
            _frame.Start();
            _frameStartedTcs.SetResult(null);
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

                var ignore = StopAsync();
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
