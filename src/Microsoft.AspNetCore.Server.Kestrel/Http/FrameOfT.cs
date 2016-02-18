// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class Frame<TContext> : Frame
    {
        private readonly IHttpApplication<TContext> _application;

        public Frame(IHttpApplication<TContext> application,
                     ConnectionContext context)
            : base(context)
        {
            _application = application;
        }

        /// <summary>
        /// Primary loop which consumes socket input, parses it for protocol framing, and invokes the
        /// application delegate for as long as the socket is intended to remain open.
        /// The resulting Task from this loop is preserved in a field which is used when the server needs
        /// to drain and close all currently active connections.
        /// </summary>
        public override async Task RequestProcessingAsync()
        {
            try
            {
                while (!_requestProcessingStopping)
                {
                    while (!_requestProcessingStopping && !TakeStartLine(SocketInput))
                    {
                        if (SocketInput.RemoteIntakeFin)
                        {
                            return;
                        }
                        await SocketInput;
                    }

                    InitializeHeaders();

                    while (!_requestProcessingStopping && !TakeMessageHeaders(SocketInput, FrameRequestHeaders))
                    {
                        if (SocketInput.RemoteIntakeFin)
                        {
                            return;
                        }
                        await SocketInput;
                    }

                    if (!_requestProcessingStopping)
                    {
                        var messageBody = MessageBody.For(HttpVersion, FrameRequestHeaders, this);
                        _keepAlive = messageBody.RequestKeepAlive;

                        InitializeStreams(messageBody);

                        _abortedCts = null;
                        _manuallySetRequestAbortToken = null;

                        var context = _application.CreateContext(this);
                        try
                        {
                            await _application.ProcessRequestAsync(context).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReportApplicationError(ex);
                        }
                        finally
                        {
                            // Trigger OnStarting if it hasn't been called yet and the app hasn't
                            // already failed. If an OnStarting callback throws we can go through
                            // our normal error handling in ProduceEnd.
                            // https://github.com/aspnet/KestrelHttpServer/issues/43
                            if (!_responseStarted && _applicationException == null && _onStarting != null)
                            {
                                await FireOnStarting();
                            }

                            PauseStreams();

                            if (_onCompleted != null)
                            {
                                await FireOnCompleted();
                            }

                            _application.DisposeContext(context, _applicationException);

                            // If _requestAbort is set, the connection has already been closed.
                            if (Volatile.Read(ref _requestAborted) == 0)
                            {
                                ResumeStreams();

                                await ProduceEnd();

                                if (_keepAlive)
                                {
                                    // Finish reading the request body in case the app did not.
                                    await messageBody.Consume();
                                }
                            }

                            StopStreams();
                        }

                        if (!_keepAlive)
                        {
                            ResetComponents(poolingPermitted: true);
                            return;
                        }
                    }

                    Reset();
                }
            }
            catch (Exception ex)
            {
                // Error occurred, do not return components to pool
                _poolingPermitted = false;
                Log.LogWarning(0, ex, "Connection processing ended abnormally");
            }
            finally
            {
                try
                {
                    ResetComponents(poolingPermitted: _poolingPermitted);
                    _abortedCts = null;

                    // If _requestAborted is set, the connection has already been closed.
                    if (Volatile.Read(ref _requestAborted) == 0)
                    {
                        ConnectionControl.End(ProduceEndType.SocketShutdown);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(0, ex, "Connection shutdown abnormally");
                }
            }
        }
    }
}
