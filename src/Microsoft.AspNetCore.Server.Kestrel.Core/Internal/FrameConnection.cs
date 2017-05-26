// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server;
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
        private List<IAdaptedConnection> _adaptedConnections;
        private readonly TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Frame _frame;

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;

        private Task _lifetimeTask;

        public FrameConnection(FrameConnectionContext context)
        {
            _context = context;
        }

        public string ConnectionId => _context.ConnectionId;
        public IPipeWriter Input => _context.Input.Writer;
        public IPipeReader Output => _context.Output.Reader;

        private PipeFactory PipeFactory => _context.ConnectionInformation.PipeFactory;

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

        public void StartRequestProcessing<TContext>(IHttpApplication<TContext> application)
        {
            _lifetimeTask = ProcessRequestsAsync<TContext>(application);
        }

        private async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            try
            {
                Log.ConnectionStart(ConnectionId);
                KestrelEventSource.Log.ConnectionStart(this, _context.ConnectionInformation);

                AdaptedPipeline adaptedPipeline = null;
                var adaptedPipelineTask = Task.CompletedTask;
                var input = _context.Input.Reader;
                var output = _context.Output;

                if (_context.ConnectionAdapters.Count > 0)
                {
                    adaptedPipeline = new AdaptedPipeline(input,
                                                          output,
                                                          PipeFactory.Create(AdaptedInputPipeOptions),
                                                          PipeFactory.Create(AdaptedOutputPipeOptions),
                                                          Log);

                    input = adaptedPipeline.Input.Reader;
                    output = adaptedPipeline.Output;
                }

                // _frame must be initialized before adding the connection to the connection manager
                _frame = new Frame<TContext>(application, new FrameContext
                {
                    ConnectionId = _context.ConnectionId,
                    ConnectionInformation = _context.ConnectionInformation,
                    ServiceContext = _context.ServiceContext,
                    TimeoutControl = this,
                    Input = input,
                    Output = output
                });

                // Do this before the first await so we don't yield control to the transport until we've
                // added the connection to the connection manager
                _context.ServiceContext.ConnectionManager.AddConnection(_context.FrameConnectionId, this);
                _lastTimestamp = _context.ServiceContext.SystemClock.UtcNow.Ticks;

                if (adaptedPipeline != null)
                {
                    // Stream can be null here and run async will close the connection in that case
                    var stream = await ApplyConnectionAdaptersAsync();
                    adaptedPipelineTask = adaptedPipeline.RunAsync(stream);
                }

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
                DisposeAdaptedConnections();

                if (_frame.WasUpgraded)
                {
                    _context.ServiceContext.ConnectionManager.UpgradedConnectionCount.ReleaseOne();
                }
                else
                {
                    _context.ServiceContext.ConnectionManager.NormalConnectionCount.ReleaseOne();
                }

                Log.ConnectionStop(ConnectionId);
                KestrelEventSource.Log.ConnectionStop(this);
            }
        }

        public void OnConnectionClosed(Exception ex)
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            // Abort the connection (if not already aborted)
            _frame.Abort(ex);

            _socketClosedTcs.TrySetResult(null);
        }

        public Task StopAsync()
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            _frame.Stop();

            return _lifetimeTask;
        }

        public void Abort(Exception ex)
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            // Abort the connection (if not already aborted)
            _frame.Abort(ex);
        }

        public Task AbortAsync(Exception ex)
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            // Abort the connection (if not already aborted)
            _frame.Abort(ex);

            return _lifetimeTask;
        }

        public void Timeout()
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            _frame.SetBadRequestState(RequestRejectionReason.RequestTimeout);
        }

        private async Task<Stream> ApplyConnectionAdaptersAsync()
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

            var features = new FeatureCollection();
            var connectionAdapters = _context.ConnectionAdapters;
            var stream = new RawStream(_context.Input.Reader, _context.Output.Writer);
            var adapterContext = new ConnectionAdapterContext(features, stream);
            _adaptedConnections = new List<IAdaptedConnection>(connectionAdapters.Count);

            try
            {
                for (var i = 0; i < connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await connectionAdapters[i].OnConnectionAsync(adapterContext);
                    _adaptedConnections.Add(adaptedConnection);
                    adapterContext = new ConnectionAdapterContext(features, adaptedConnection.ConnectionStream);
                }
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");

                return null;
            }
            finally
            {
                _frame.ConnectionFeatures = features;
            }

            return adapterContext.ConnectionStream;
        }

        private void DisposeAdaptedConnections()
        {
            var adaptedConnections = _adaptedConnections;
            if (adaptedConnections != null)
            {
                for (int i = adaptedConnections.Count - 1; i >= 0; i--)
                {
                    adaptedConnections[i].Dispose();
                }
            }
        }

        public void Tick(DateTimeOffset now)
        {
            Debug.Assert(_frame != null, $"nameof({_frame}) is null");

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
