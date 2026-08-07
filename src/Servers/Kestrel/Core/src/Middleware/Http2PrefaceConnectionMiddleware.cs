// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class Http2PrefaceConnectionMiddleware
{
    private readonly ConnectionDelegate _next;
    private readonly HttpProtocols _endpointDefaultProtocols;
    private readonly TimeSpan _keepAliveTimeout;
    private readonly KestrelTrace _log;
    private readonly CancellationTokenSourcePool _ctsPool = new();

    public Http2PrefaceConnectionMiddleware(
        ConnectionDelegate next,
        ServiceContext serviceContext,
        HttpProtocols endpointDefaultProtocols)
    {
        _next = next;
        _endpointDefaultProtocols = endpointDefaultProtocols;
        _keepAliveTimeout = serviceContext.ServerOptions.Limits.KeepAliveTimeout;
        _log = serviceContext.Log;
    }

    public Task OnConnectionAsync(ConnectionContext connectionContext)
    {
        var protocols = connectionContext.Features.Get<HttpProtocolsFeature>()?.HttpProtocols ?? _endpointDefaultProtocols;

        if (connectionContext.Features.Get<ITlsConnectionFeature>() is not null ||
            !protocols.HasFlag(HttpProtocols.Http1) ||
            !protocols.HasFlag(HttpProtocols.Http2))
        {
            return _next(connectionContext);
        }

        return SelectProtocolAsync(connectionContext);
    }

    private async Task SelectProtocolAsync(ConnectionContext connectionContext)
    {
        var input = connectionContext.Transport.Input;
        var selectedProtocol = HttpProtocols.None;
        var lifetimeNotificationFeature = connectionContext.Features.Get<IConnectionLifetimeNotificationFeature>();
        var shutdownToken = lifetimeNotificationFeature?.ConnectionClosedRequested ?? default;

        using (var cancellationTokenSource = _ctsPool.Rent())
        using (shutdownToken.UnsafeRegister(static state => ((CancellationTokenSource)state!).Cancel(), cancellationTokenSource))
        {
            if (_keepAliveTimeout != TimeSpan.MaxValue)
            {
                cancellationTokenSource.CancelAfter(_keepAliveTimeout);
            }

            while (true)
            {
                ReadResult result;
                try
                {
                    result = await input.ReadAsync(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
                {
                    if (!shutdownToken.IsCancellationRequested)
                    {
                        RecordKeepAliveTimeout(connectionContext);
                    }
                    return;
                }
                catch (ConnectionResetException)
                {
                    return;
                }
                catch (IOException ex)
                {
                    _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                    KestrelMetrics.AddConnectionEndReason(
                        connectionContext.Features.Get<IConnectionMetricsTagsFeature>(),
                        ConnectionEndReason.IOError);
                    return;
                }
                catch (ConnectionAbortedException ex)
                {
                    _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                    return;
                }

                var buffer = result.Buffer;
                var examined = buffer.Start;
                var inputCompleted = false;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        var compareLength = (int)Math.Min(buffer.Length, Http2Connection.ClientPreface.Length);
                        var reader = new SequenceReader<byte>(buffer);

                        if (!reader.IsNext(Http2Connection.ClientPreface[..compareLength], advancePast: false))
                        {
                            selectedProtocol = HttpProtocols.Http1;
                        }
                        else if (buffer.Length >= Http2Connection.ClientPreface.Length)
                        {
                            selectedProtocol = HttpProtocols.Http2;
                        }
                        else if (result.IsCompleted)
                        {
                            selectedProtocol = HttpProtocols.Http1;
                        }
                        else
                        {
                            examined = buffer.End;
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        inputCompleted = true;
                    }
                }
                finally
                {
                    input.AdvanceTo(buffer.Start, examined);
                }

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    if (!shutdownToken.IsCancellationRequested)
                    {
                        RecordKeepAliveTimeout(connectionContext);
                    }
                    return;
                }

                if (inputCompleted)
                {
                    return;
                }

                if (selectedProtocol != HttpProtocols.None)
                {
                    connectionContext.Features.Set(new HttpProtocolsFeature(selectedProtocol));
                    break;
                }
            }
        }

        await _next(connectionContext);
    }

    private static void RecordKeepAliveTimeout(ConnectionContext connectionContext)
    {
        KestrelMetrics.AddConnectionEndReason(
            connectionContext.Features.Get<IConnectionMetricsTagsFeature>(),
            ConnectionEndReason.KeepAliveTimeout);
    }
}
