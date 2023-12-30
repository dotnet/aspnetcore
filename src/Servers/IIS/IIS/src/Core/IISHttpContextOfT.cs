// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Core;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

internal sealed class IISHttpContextOfT<TContext> : IISHttpContext where TContext : notnull
{
    private readonly IHttpApplication<TContext> _application;

    public IISHttpContextOfT(MemoryPool<byte> memoryPool, IHttpApplication<TContext> application, NativeSafeHandle pInProcessHandler, IISServerOptions options, IISHttpServer server, ILogger logger, bool useLatin1)
        : base(memoryPool, pInProcessHandler, options, server, logger, useLatin1)
    {
        _application = application;
    }

    public override async Task<bool> ProcessRequestAsync()
    {
        var context = default(TContext);
        var success = true;

        try
        {
            InitializeContext();

            try
            {
                context = _application.CreateContext(this);

                await _application.ProcessRequestAsync(context);
            }
            catch (BadHttpRequestException ex)
            {
                SetBadRequestState(ex);
                ReportApplicationError(ex);
                success = false;
            }
            catch (Exception ex)
            {
                if ((ex is OperationCanceledException || ex is IOException) && ClientDisconnected)
                {
                    ReportRequestAborted();
                }
                else
                {
                    ReportApplicationError(ex);
                }

                success = false;
            }

            if (ResponsePipeWrapper != null)
            {
                await ResponsePipeWrapper.CompleteAsync();
            }

            _streams.Stop();

            if (!HasResponseStarted && _applicationException == null && _onStarting != null)
            {
                await FireOnStarting();
                // Dispose
            }

            if (!success && HasResponseStarted && AdvancedHttp2FeaturesSupported())
            {
                // HTTP/2 INTERNAL_ERROR = 0x2 https://tools.ietf.org/html/rfc7540#section-7
                // Otherwise the default is Cancel = 0x8 (h2) or 0x010c (h3).
                if (HttpVersion == System.Net.HttpVersion.Version20)
                {
                    // HTTP/2 INTERNAL_ERROR = 0x2 https://tools.ietf.org/html/rfc7540#section-7
                    SetResetCode(2);
                }
                else if (HttpVersion == System.Net.HttpVersion.Version30)
                {
                    // HTTP/3 H3_INTERNAL_ERROR = 0x0102 https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-8.1
                    SetResetCode(0x0102);
                }
            }

            if (!_requestAborted)
            {
                await ProduceEnd();
            }
            else if (!HasResponseStarted && _requestRejectedException == null)
            {
                // If the request was aborted and no response was sent, we use status code 499 for logging               
                StatusCode = ClientDisconnected ? StatusCodes.Status499ClientClosedRequest : 0;
                success = false;
            }

            // Complete response writer and request reader pipe sides
            _bodyOutput.Complete();
            _bodyInputPipe?.Reader.Complete();

            // Allow writes to drain
            if (_writeBodyTask != null)
            {
                await _writeBodyTask;
            }

            // Cancel all remaining IO, there might be reads pending if not entire request body was sent by client
            AsyncIO?.Complete();

            if (_readBodyTask != null)
            {
                await _readBodyTask;
            }
        }
        catch (Exception ex)
        {
            success = false;
            ReportApplicationError(ex);
        }
        finally
        {
            if (_onCompleted != null)
            {
                await FireOnCompleted();
            }

            if (context != null)
            {
                try
                {
                    _application.DisposeContext(context, _applicationException);
                }
                catch (Exception ex)
                {
                    ReportApplicationError(ex);
                }
            }
        }
        return success;
    }
}
