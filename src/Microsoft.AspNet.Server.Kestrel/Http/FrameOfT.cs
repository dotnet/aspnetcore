// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Frame<TContext> : Frame
    {
        private readonly IHttpApplication<TContext> _application;

        public Frame(IHttpApplication<TContext> application,
                     ConnectionContext context)
            : this(application, context, remoteEndPoint: null, localEndPoint: null, prepareRequest: null)
        {
        }

        public Frame(IHttpApplication<TContext> application,
                     ConnectionContext context,
                     IPEndPoint remoteEndPoint,
                     IPEndPoint localEndPoint,
                     Action<IFeatureCollection> prepareRequest)
            : base(context, remoteEndPoint, localEndPoint, prepareRequest)
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
                var terminated = false;
                while (!terminated && !_requestProcessingStopping)
                {
                    while (!terminated && !_requestProcessingStopping && !TakeStartLine(SocketInput))
                    {
                        terminated = SocketInput.RemoteIntakeFin;
                        if (!terminated)
                        {
                            await SocketInput;
                        }
                    }

                    while (!terminated && !_requestProcessingStopping && !TakeMessageHeaders(SocketInput, _requestHeaders))
                    {
                        terminated = SocketInput.RemoteIntakeFin;
                        if (!terminated)
                        {
                            await SocketInput;
                        }
                    }

                    if (!terminated && !_requestProcessingStopping)
                    {
                        var messageBody = MessageBody.For(HttpVersion, _requestHeaders, this);
                        _keepAlive = messageBody.RequestKeepAlive;
                        _requestBody = new FrameRequestStream(messageBody);
                        RequestBody = _requestBody;
                        _responseBody = new FrameResponseStream(this);
                        ResponseBody = _responseBody;
                        DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);

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
                            if (!_responseStarted && _applicationException == null)
                            {
                                await FireOnStarting();
                            }

                            await FireOnCompleted();

                            _application.DisposeContext(context, _applicationException);

                            // If _requestAbort is set, the connection has already been closed.
                            if (!_requestAborted)
                            {
                                await ProduceEnd();

                                if (_keepAlive)
                                {
                                    // Finish reading the request body in case the app did not.
                                    await messageBody.Consume();
                                }
                            }

                            _requestBody.StopAcceptingReads();
                            _responseBody.StopAcceptingWrites();
                        }

                        terminated = !_keepAlive;
                    }

                    Reset();
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("Connection processing ended abnormally", ex);
            }
            finally
            {
                try
                {
                    _abortedCts = null;

                    // If _requestAborted is set, the connection has already been closed.
                    if (!_requestAborted)
                    {
                        // Inform client no more data will ever arrive
                        ConnectionControl.End(ProduceEndType.SocketShutdownSend);

                        // Wait for client to either disconnect or send unexpected data
                        await SocketInput;

                        // Dispose socket
                        ConnectionControl.End(ProduceEndType.SocketDisconnect);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning("Connection shutdown abnormally", ex);
                }
            }
        }
    }
}
