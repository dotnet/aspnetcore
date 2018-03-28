// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client.Internal
{
    public partial class ServerSentEventsTransport : ITransport
    {
        private readonly HttpClient _httpClient;
        private readonly HttpOptions _httpOptions;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
        private readonly ServerSentEventsMessageParser _parser = new ServerSentEventsMessageParser();

        private IDuplexPipe _application;

        public Task Running { get; private set; } = Task.CompletedTask;

        public ServerSentEventsTransport(HttpClient httpClient)
            : this(httpClient, null, null)
        { }

        public ServerSentEventsTransport(HttpClient httpClient, HttpOptions httpOptions, ILoggerFactory loggerFactory)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(_httpClient));
            }

            _httpClient = httpClient;
            _httpOptions = httpOptions;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ServerSentEventsTransport>();
        }

        public Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection)
        {
            if (transferFormat != TransferFormat.Text)
            {
                throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
            }

            _application = application;

            Log.StartTransport(_logger, transferFormat);

            var startTcs = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);
            var sendTask = SendUtils.SendMessages(url, _application, _httpClient, _httpOptions, _transportCts, _logger);
            var receiveTask = OpenConnection(_application, url, startTcs, _transportCts.Token);

            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                Log.TransportStopped(_logger, t.Exception?.InnerException);
                _application.Output.Complete(t.Exception?.InnerException);
                _application.Input.Complete();

                return t;
            }).Unwrap();

            return startTcs.Task;
        }

        private async Task OpenConnection(IDuplexPipe application, Uri url, TaskCompletionSource<object> startTcs, CancellationToken cancellationToken)
        {
            Log.StartReceive(_logger);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SendUtils.PrepareHttpRequest(request, _httpOptions);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                startTcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                Log.TransportStopping(_logger);
                startTcs.TrySetException(ex);
                return;
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var pipeOptions = new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0);
                var pipelineReader = StreamPipeConnection.CreateReader(pipeOptions, stream);
                var readCancellationRegistration = cancellationToken.Register(
                    reader => ((PipeReader)reader).CancelPendingRead(), pipelineReader);
                try
                {
                    while (true)
                    {
                        var result = await pipelineReader.ReadAsync();
                        var input = result.Buffer;
                        if (result.IsCanceled || (input.IsEmpty && result.IsCompleted))
                        {
                            Log.EventStreamEnded(_logger);
                            break;
                        }

                        var consumed = input.Start;
                        var examined = input.End;
                        try
                        {
                            Log.ParsingSSE(_logger, input.Length);
                            var parseResult = _parser.ParseMessage(input, out consumed, out examined, out var buffer);

                            switch (parseResult)
                            {
                                case ServerSentEventsMessageParser.ParseResult.Completed:
                                    Log.MessageToApp(_logger, buffer.Length);
                                    await _application.Output.WriteAsync(buffer);
                                    _parser.Reset();
                                    break;
                                case ServerSentEventsMessageParser.ParseResult.Incomplete:
                                    if (result.IsCompleted)
                                    {
                                        throw new FormatException("Incomplete message.");
                                    }
                                    break;
                            }
                        }
                        finally
                        {
                            pipelineReader.AdvanceTo(consumed, examined);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.ReceiveCanceled(_logger);
                }
                finally
                {
                    readCancellationRegistration.Dispose();
                    _transportCts.Cancel();
                    Log.ReceiveStopped(_logger);
                }
            }
        }

        public async Task StopAsync()
        {
            Log.TransportStopping(_logger);
            _transportCts.Cancel();

            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }
        }
    }
}
