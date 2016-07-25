// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
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
                    while (!_requestProcessingStopping && TakeStartLine(SocketInput) != RequestLineStatus.Done)
                    {
                        if (SocketInput.CheckFinOrThrow())
                        {
                            // We need to attempt to consume start lines and headers even after
                            // SocketInput.RemoteIntakeFin is set to true to ensure we don't close a
                            // connection without giving the application a chance to respond to a request
                            // sent immediately before the a FIN from the client.
                            var requestLineStatus = TakeStartLine(SocketInput);

                            if (requestLineStatus == RequestLineStatus.Empty)
                            {
                                return;
                            }

                            if (requestLineStatus != RequestLineStatus.Done)
                            {
                                RejectRequest(RequestRejectionReason.MalformedRequestLineStatus, requestLineStatus.ToString());
                            }

                            break;
                        }

                        await SocketInput;
                    }

                    InitializeHeaders();

                    while (!_requestProcessingStopping && !TakeMessageHeaders(SocketInput, FrameRequestHeaders))
                    {
                        if (SocketInput.CheckFinOrThrow())
                        {
                            // We need to attempt to consume start lines and headers even after
                            // SocketInput.RemoteIntakeFin is set to true to ensure we don't close a
                            // connection without giving the application a chance to respond to a request
                            // sent immediately before the a FIN from the client.
                            if (!TakeMessageHeaders(SocketInput, FrameRequestHeaders))
                            {
                                RejectRequest(RequestRejectionReason.MalformedRequestInvalidHeaders);
                            }

                            break;
                        }

                        await SocketInput;
                    }

                    if (!_requestProcessingStopping)
                    {
                        var messageBody = MessageBody.For(_httpVersion, FrameRequestHeaders, this);
                        _keepAlive = messageBody.RequestKeepAlive;

                        InitializeStreams(messageBody);

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
                            if (!HasResponseStarted && _applicationException == null && _onStarting != null)
                            {
                                await FireOnStarting();
                            }

                            PauseStreams();

                            if (_onCompleted != null)
                            {
                                await FireOnCompleted();
                            }

                            _application.DisposeContext(context, _applicationException);
                        }

                        // If _requestAbort is set, the connection has already been closed.
                        if (Volatile.Read(ref _requestAborted) == 0)
                        {
                            ResumeStreams();

                            if (_keepAlive)
                            {
                                // Finish reading the request body in case the app did not.
                                await messageBody.Consume();
                            }

                            await ProduceEnd();
                        }

                        StopStreams();

                        if (!_keepAlive)
                        {
                            // End the connection for non keep alive as data incoming may have been thrown off
                            return;
                        }
                    }

                    Reset();
                }
            }
            catch (BadHttpRequestException ex)
            {
                if (!_requestRejected)
                {
                    // SetBadRequestState logs the error.
                    SetBadRequestState(ex);
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning(0, ex, "Connection processing ended abnormally");
            }
            finally
            {
                try
                {
                    await TryProduceInvalidRequestResponse();

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
