// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class LongPollingTransport : ITransport
{
    private const string AuthenticationRefreshRequiredError = "authentication_refresh_required";

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly HttpConnectionOptions _httpConnectionOptions;
    private readonly Func<CancellationToken, Task>? _authenticationRefresh;
    private IDuplexPipe? _application;
    private IDuplexPipe? _transport;
    // Volatile so that the poll loop sees the updated value set from a different thread
    private volatile Exception? _error;

    private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();

    internal Task Running { get; private set; } = Task.CompletedTask;

    public PipeReader Input => _transport!.Input;

    public PipeWriter Output => _transport!.Output;

    public LongPollingTransport(
        HttpClient httpClient,
        HttpConnectionOptions? httpConnectionOptions = null,
        ILoggerFactory? loggerFactory = null,
        Func<CancellationToken, Task>? authenticationRefresh = null)
    {
        _httpClient = httpClient;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(typeof(LongPollingTransport));
        _httpConnectionOptions = httpConnectionOptions ?? new();
        _authenticationRefresh = authenticationRefresh;
    }

    public async Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default)
    {
        if (transferFormat != TransferFormat.Binary && transferFormat != TransferFormat.Text)
        {
            throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
        }

        Log.StartTransport(_logger, transferFormat);

        // Make initial long polling request
        // Server uses first long polling request to finish initializing connection and it returns without data
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
        }

        // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
        var pair = DuplexPipe.CreateConnectionPair(_httpConnectionOptions.TransportPipeOptions, _httpConnectionOptions.AppPipeOptions);

        _transport = pair.Transport;
        _application = pair.Application;

        Running = ProcessAsync(url);
    }

    private async Task ProcessAsync(Uri url)
    {
        Debug.Assert(_application != null);

        // Start sending and polling (ask for binary if the server supports it)
        var receiving = Poll(url, _transportCts.Token);
        var sending = SendUtils.SendMessages(url, _application, _httpClient, _logger);

        // Wait for send or receive to complete
        var trigger = await Task.WhenAny(receiving, sending).ConfigureAwait(false);

        if (trigger == receiving)
        {
            // We don't need to DELETE here because the poll completed, which means the server shut down already.

            // We're waiting for the application to finish and there are 2 things it could be doing
            // 1. Waiting for application data
            // 2. Waiting for an outgoing send (this should be instantaneous)

            // Cancel the application so that ReadAsync yields
            _application.Input.CancelPendingRead();

            await sending.ConfigureAwait(false);
        }
        else
        {
            // Set the sending error so we communicate that to the application
            _error = sending.IsFaulted ? sending.Exception!.InnerException : null;

            // Cancel the poll request
            _transportCts.Cancel();

            // Cancel any pending flush so that we can quit
            _application.Output.CancelPendingFlush();

            await receiving.ConfigureAwait(false);

            // Send the DELETE request to clean-up the connection on the server.
            await SendDeleteRequest(url).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        Log.TransportStopping(_logger);

        if (_application == null)
        {
            // We never started
            return;
        }

        _application.Input.CancelPendingRead();

        try
        {
            await Running.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.TransportStopped(_logger, ex);
            throw;
        }

        _transport!.Output.Complete();
        _transport!.Input.Complete();

        Log.TransportStopped(_logger, null);
    }

    private async Task Poll(Uri pollUrl, CancellationToken cancellationToken)
    {
        Debug.Assert(_application != null);

        Log.StartReceive(_logger);

        // Allocate this once for the duration of the transport so we can continuously write to it
        var applicationStream = new PipeWriterStream(_application.Output);
        var authenticationRefreshAttempted = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, pollUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                HttpResponseMessage response;

                try
                {
                    response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // SendAsync will throw the OperationCanceledException if the passed cancellationToken is canceled
                    // or if the http request times out due to HttpClient.Timeout expiring. In the latter case we
                    // just want to start a new poll.
                    continue;
                }
                catch (WebException ex) when (!OperatingSystem.IsBrowser() && ex.Status == WebExceptionStatus.RequestCanceled)
                {
                    // SendAsync on .NET Framework doesn't reliably throw OperationCanceledException.
                    // Catch the WebException and test it.
                    // https://github.com/dotnet/corefx/issues/26335
                    continue;
                }

                using (response)
                {
                    Log.PollResponseReceived(_logger, response);

                    if (!authenticationRefreshAttempted
                        && _authenticationRefresh is not null
                        && await IsAuthenticationRefreshRequiredAsync(response, cancellationToken).ConfigureAwait(false))
                    {
                        authenticationRefreshAttempted = true;
                        await _authenticationRefresh(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    authenticationRefreshAttempted = false;
                    response.EnsureSuccessStatusCode();

                    if (response.StatusCode == HttpStatusCode.NoContent || cancellationToken.IsCancellationRequested)
                    {
                        Log.ClosingConnection(_logger);

                        // Transport closed or polling stopped, we're done
                        break;
                    }

                    Log.ReceivedMessages(_logger);
#if NETCOREAPP
                    await response.Content.CopyToAsync(applicationStream, cancellationToken).ConfigureAwait(false);

#else
                    await response.Content.CopyToAsync(applicationStream).ConfigureAwait(false);
#endif

                    var flushResult = await _application.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

                    // We canceled in the middle of applying back pressure
                    // or if the consumer is done
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.ErrorPolling(_logger, pollUrl, ex);

            _error = ex;
        }
        catch (OperationCanceledException)
        {
            // transport is being closed
            Log.ReceiveCanceled(_logger);
        }
        catch (Exception ex)
        {
            Log.ErrorPolling(_logger, pollUrl, ex);

            _error = ex;
        }
        finally
        {
            _application.Output.Complete(_error);

            Log.ReceiveStopped(_logger);
        }
    }

    private static async Task<bool> IsAuthenticationRefreshRequiredAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Conflict)
        {
            return false;
        }

#if NET5_0_OR_GREATER
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
        var content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#pragma warning restore CA2016
#endif

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String
                && error.GetString() == AuthenticationRefreshRequiredError;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task SendDeleteRequest(Uri url)
    {
        try
        {
            Log.SendingDeleteRequest(_logger, url);
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.ConnectionAlreadyClosedSendingDeleteRequest(_logger, url);
            }
            else
            {
                // Check for non-404 errors
                response.EnsureSuccessStatusCode();
                Log.DeleteRequestAccepted(_logger, url);
            }
        }
        catch (Exception ex)
        {
            Log.ErrorSendingDeleteRequest(_logger, url, ex);
        }
    }
}
