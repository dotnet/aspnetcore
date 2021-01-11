// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal partial class LongPollingTransport : ITransport
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private IDuplexPipe? _application;
        private IDuplexPipe? _transport;
        // Volatile so that the poll loop sees the updated value set from a different thread
        private volatile Exception? _error;

        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();

        internal Task Running { get; private set; } = Task.CompletedTask;

        public PipeReader Input => _transport!.Input;

        public PipeWriter Output => _transport!.Output;

        public LongPollingTransport(HttpClient httpClient)
            : this(httpClient, null)
        { }

        public LongPollingTransport(HttpClient httpClient, ILoggerFactory? loggerFactory)
        {
            _httpClient = httpClient;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LongPollingTransport>();
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
            using (var response = await _httpClient.SendAsync(request, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }

            // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
            var options = ClientPipeOptions.DefaultOptions;
            var pair = DuplexPipe.CreateConnectionPair(options, options);

            _transport = pair.Transport;
            _application = pair.Application;

            Running = ProcessAsync(url);
        }

        private async Task ProcessAsync(Uri url)
        {
            // Start sending and polling (ask for binary if the server supports it)
            var receiving = Poll(url, _transportCts.Token);
            var sending = SendUtils.SendMessages(url, _application!, _httpClient, _logger);

            // Wait for send or receive to complete
            var trigger = await Task.WhenAny(receiving, sending);

            if (trigger == receiving)
            {
                // We don't need to DELETE here because the poll completed, which means the server shut down already.

                // We're waiting for the application to finish and there are 2 things it could be doing
                // 1. Waiting for application data
                // 2. Waiting for an outgoing send (this should be instantaneous)

                // Cancel the application so that ReadAsync yields
                _application!.Input.CancelPendingRead();

                await sending;
            }
            else
            {
                // Set the sending error so we communicate that to the application
                _error = sending.IsFaulted ? sending.Exception!.InnerException : null;

                // Cancel the poll request
                _transportCts.Cancel();

                // Cancel any pending flush so that we can quit
                _application!.Output.CancelPendingFlush();

                await receiving;

                // Send the DELETE request to clean-up the connection on the server.
                await SendDeleteRequest(url);
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
                await Running;
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
            Log.StartReceive(_logger);

            // Allocate this once for the duration of the transport so we can continuously write to it
            var applicationStream = new PipeWriterStream(_application!.Output);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, pollUrl);

                    HttpResponseMessage response;

                    try
                    {
                        response = await _httpClient.SendAsync(request, cancellationToken);
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

                    Log.PollResponseReceived(_logger, response);

                    response.EnsureSuccessStatusCode();

                    if (response.StatusCode == HttpStatusCode.NoContent || cancellationToken.IsCancellationRequested)
                    {
                        Log.ClosingConnection(_logger);

                        // Transport closed or polling stopped, we're done
                        break;
                    }
                    else
                    {
                        Log.ReceivedMessages(_logger);

                        await response.Content.CopyToAsync(applicationStream);
                        var flushResult = await _application.Output.FlushAsync();

                        // We canceled in the middle of applying back pressure
                        // or if the consumer is done
                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                        {
                            break;
                        }
                    }
                }
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

        private async Task SendDeleteRequest(Uri url)
        {
            try
            {
                Log.SendingDeleteRequest(_logger, url);
                var response = await _httpClient.DeleteAsync(url);

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
}
