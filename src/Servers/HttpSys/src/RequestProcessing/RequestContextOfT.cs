// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Log = Microsoft.AspNetCore.Server.HttpSys.RequestContextLog;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class RequestContext<TContext> : RequestContext where TContext : notnull
{
    private readonly IHttpApplication<TContext> _application;
    private readonly MessagePump _messagePump;

    public RequestContext(IHttpApplication<TContext> application, MessagePump messagePump, HttpSysListener server, uint? bufferSize, ulong requestId)
        : base(server, bufferSize, requestId)
    {
        _application = application;
        _messagePump = messagePump;
    }

    public override async Task ExecuteAsync()
    {
        var messagePump = _messagePump;
        var application = _application;

        try
        {
            InitializeFeatures();

            if (messagePump.Stopping)
            {
                SetFatalResponse(503);
                Dispose();
                return;
            }

            TContext? context = default;
            Exception? applicationException = null;
            messagePump.IncrementOutstandingRequest();
            try
            {
                context = application.CreateContext(Features);
                try
                {
                    await application.ProcessRequestAsync(context);
                    await CompleteAsync();
                }
                finally
                {
                    await OnCompleted();
                }
            }
            catch (Exception ex)
            {
                applicationException = ex;

                if ((ex is OperationCanceledException || ex is IOException) && DisconnectToken.IsCancellationRequested)
                {
                    Log.RequestAborted(Logger);
                }
                else
                {
                    Log.RequestProcessError(Logger, ex);
                }
                if (Response.HasStarted)
                {
                    // Otherwise the default is Cancel = 0x8 (h2) or 0x010c (h3).
                    if (Request.ProtocolVersion == HttpVersion.Version20)
                    {
                        // HTTP/2 INTERNAL_ERROR = 0x2 https://tools.ietf.org/html/rfc7540#section-7
                        SetResetCode(2);
                    }
                    else if (Request.ProtocolVersion == HttpVersion.Version30)
                    {
                        // HTTP/3 H3_INTERNAL_ERROR = 0x0102 https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-8.1
                        SetResetCode(0x0102);
                    }
                    Abort();
                }
                else
                {
                    // We haven't sent a response yet, try to send a 500 Internal Server Error
                    Response.Headers.IsReadOnly = false;
                    Response.Trailers.IsReadOnly = false;
                    Response.Headers.Clear();
                    Response.Trailers.Clear();

                    if (ex is BadHttpRequestException badHttpRequestException)
                    {
                        SetFatalResponse(badHttpRequestException.StatusCode);
                    }
                    else if ((ex is OperationCanceledException || ex is IOException) && DisconnectToken.IsCancellationRequested)
                    {
                        SetFatalResponse(StatusCodes.Status499ClientClosedRequest);
                    }
                    else
                    {
                        SetFatalResponse(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            finally
            {
                if (context != null)
                {
                    application.DisposeContext(context, applicationException);
                }

                if (messagePump.DecrementOutstandingRequest() == 0 && messagePump.Stopping)
                {
                    Log.RequestsDrained(Logger);
                    messagePump.SetShutdownSignal();
                }

                Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.RequestError(Logger, ex);
            Abort();
        }
    }
}
