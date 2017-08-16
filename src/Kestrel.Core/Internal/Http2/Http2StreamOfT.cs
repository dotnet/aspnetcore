// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2Stream<TContext> : Http2Stream
    {
        private readonly IHttpApplication<TContext> _application;

        public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context)
            : base(context)
        {
            _application = application;
        }

        public override async Task ProcessRequestAsync()
        {
            try
            {
                Method = RequestHeaders[":method"];
                Scheme = RequestHeaders[":scheme"];

                var path = RequestHeaders[":path"].ToString();
                var queryIndex = path.IndexOf('?');

                Path = queryIndex == -1 ? path : path.Substring(0, queryIndex);
                QueryString = queryIndex == -1 ? string.Empty : path.Substring(queryIndex);

                RequestHeaders["Host"] = RequestHeaders[":authority"];

                // TODO: figure out what the equivalent for HTTP/2 is
                // EnsureHostHeaderExists();

                MessageBody = Http2MessageBody.For(FrameRequestHeaders, this);

                InitializeStreams(MessageBody);

                var context = _application.CreateContext(this);
                try
                {
                    try
                    {
                        //KestrelEventSource.Log.RequestStart(this);

                        await _application.ProcessRequestAsync(context);

                        if (Volatile.Read(ref _requestAborted) == 0)
                        {
                            VerifyResponseContentLength();
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportApplicationError(ex);

                        if (ex is BadHttpRequestException)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        //KestrelEventSource.Log.RequestStop(this);

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
                    }

                    // If _requestAbort is set, the connection has already been closed.
                    if (Volatile.Read(ref _requestAborted) == 0)
                    {
                        await ProduceEnd();
                    }
                    else if (!HasResponseStarted)
                    {
                        // If the request was aborted and no response was sent, there's no
                        // meaningful status code to log.
                        StatusCode = 0;
                    }
                }
                catch (BadHttpRequestException ex)
                {
                    // Handle BadHttpRequestException thrown during app execution or remaining message body consumption.
                    // This has to be caught here so StatusCode is set properly before disposing the HttpContext
                    // (DisposeContext logs StatusCode).
                    SetBadRequestState(ex);
                }
                finally
                {
                    _application.DisposeContext(context, _applicationException);

                    // StopStreams should be called before the end of the "if (!_requestProcessingStopping)" block
                    // to ensure InitializeStreams has been called.
                    StopStreams();

                    if (HasStartedConsumingRequestBody)
                    {
                        RequestBodyPipe.Reader.Complete();

                        // Wait for MessageBody.PumpAsync() to call RequestBodyPipe.Writer.Complete().
                        await MessageBody.StopAsync();

                        // At this point both the request body pipe reader and writer should be completed.
                        RequestBodyPipe.Reset();
                    }
                }
            }
            catch (BadHttpRequestException ex)
            {
                // Handle BadHttpRequestException thrown during request line or header parsing.
                // SetBadRequestState logs the error.
                SetBadRequestState(ex);
            }
            catch (ConnectionResetException ex)
            {
                // Don't log ECONNRESET errors made between requests. Browsers like IE will reset connections regularly.
                if (_requestProcessingStatus != RequestProcessingStatus.RequestPending)
                {
                    Log.RequestProcessingError(ConnectionId, ex);
                }
            }
            catch (IOException ex)
            {
                Log.RequestProcessingError(ConnectionId, ex);
            }
            catch (Exception ex)
            {
                Log.LogWarning(0, ex, CoreStrings.RequestProcessingEndError);
            }
            finally
            {
                try
                {
                    if (Volatile.Read(ref _requestAborted) == 0)
                    {
                        await TryProduceInvalidRequestResponse();
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(0, ex, CoreStrings.ConnectionShutdownError);
                }
                finally
                {
                    StreamLifetimeHandler.OnStreamCompleted(StreamId);
                }
            }
        }
    }
}
