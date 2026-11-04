// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class ServerSentEventsTransport : ITransport
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly HttpConnectionOptions _httpConnectionOptions;
    // Volatile so that the SSE loop sees the updated value set from a different thread
    private volatile Exception? _error;
    private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
    private readonly CancellationTokenSource _inputCts = new CancellationTokenSource();
    private IDuplexPipe? _transport;
    private IDuplexPipe? _application;

    internal Task Running { get; private set; } = Task.CompletedTask;

    public PipeReader Input => _transport!.Input;

    public PipeWriter Output => _transport!.Output;

    public ServerSentEventsTransport(HttpClient httpClient, HttpConnectionOptions? httpConnectionOptions = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(typeof(ServerSentEventsTransport));
        _httpConnectionOptions = httpConnectionOptions ?? new();
    }

    public async Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default)
    {
        if (transferFormat != TransferFormat.Text)
        {
            throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
        }

        Log.StartTransport(_logger, transferFormat);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage? response = null;

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            response?.Dispose();

            Log.TransportStopping(_logger);

            throw;
        }

        // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
        var pair = DuplexPipe.CreateConnectionPair(_httpConnectionOptions.TransportPipeOptions, _httpConnectionOptions.AppPipeOptions);

        _transport = pair.Transport;
        _application = pair.Application;

        // Cancellation token will be triggered when the pipe is stopped on the client.
        // This is to avoid the client throwing from a 404 response caused by the
        // server stopping the connection while the send message request is in progress.
        // _application.Input.OnWriterCompleted((exception, state) => ((CancellationTokenSource)state).Cancel(), inputCts);

        Running = ProcessAsync(url, response);
    }

    private async Task ProcessAsync(Uri url, HttpResponseMessage response)
    {
        Debug.Assert(_application != null);

        // Start sending and polling (ask for binary if the server supports it)
        var receiving = ProcessEventStream(response, _transportCts.Token);
        var sending = SendUtils.SendMessages(url, _application, _httpClient, _logger, _inputCts.Token);

        // Wait for send or receive to complete
        var trigger = await Task.WhenAny(receiving, sending).ConfigureAwait(false);

        if (trigger == receiving)
        {
            // We're waiting for the application to finish and there are 2 things it could be doing
            // 1. Waiting for application data
            // 2. Waiting for an outgoing send (this should be instantaneous)

            _inputCts.Cancel();

            // Cancel the application so that ReadAsync yields
            _application.Input.CancelPendingRead();

            await sending.ConfigureAwait(false);
        }
        else
        {
            // Set the sending error so we communicate that to the application
            _error = sending.IsFaulted ? sending.Exception!.InnerException : null;

            _transportCts.Cancel();

            // Cancel any pending flush so that we can quit
            _application.Output.CancelPendingFlush();

            await receiving.ConfigureAwait(false);
        }
    }

    private async Task ProcessEventStream(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Debug.Assert(_application != null);

        Log.StartReceive(_logger);

        using (response)
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
#pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods
        {
            try
            {
                var parser = SseParser.Create(stream, (eventType, bytes) => bytes.ToArray());
                await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
                {
                    Log.MessageToApplication(_logger, item.Data.Length);

                    // When cancellationToken is canceled the next line will cancel pending flushes on the pipe unblocking the await.
                    // Avoid passing the passed in context.
                    var flushResult = await _application.Output.WriteAsync(item.Data, default).ConfigureAwait(false);

                    // We canceled in the middle of applying back pressure
                    // or if the consumer is done
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        Log.EventStreamEnded(_logger);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.ReceiveCanceled(_logger);
            }
            catch (Exception ex)
            {
                _error = ex;
            }
            finally
            {
                _application.Output.Complete(_error);

                Log.ReceiveStopped(_logger);
            }
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

        _transport!.Output.Complete();
        _transport!.Input.Complete();

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

        Log.TransportStopped(_logger, null);
    }
}
