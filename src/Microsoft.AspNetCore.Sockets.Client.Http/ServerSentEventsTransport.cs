// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class ServerSentEventsTransport : ITransport
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
        private readonly ServerSentEventsMessageParser _parser = new ServerSentEventsMessageParser();

        private Channel<byte[], SendMessage> _application;

        public Task Running { get; private set; } = Task.CompletedTask;

        public ServerSentEventsTransport(HttpClient httpClient)
            : this(httpClient, null)
        { }

        public ServerSentEventsTransport(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(_httpClient));
            }

            _httpClient = httpClient;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ServerSentEventsTransport>();
        }

        public Task StartAsync(Uri url, Channel<byte[], SendMessage> application)
        {
            _logger.LogInformation("Starting {transportName}", nameof(ServerSentEventsTransport));

            _application = application;
            var sendTask = SendUtils.SendMessages(url, _application, _httpClient, _transportCts, _logger);
            var receiveTask = OpenConnection(_application, url, _transportCts.Token);

            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogError(0, t.Exception.InnerException, "Transport stopped");
                }

                _application.Out.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();

            return Task.CompletedTask;
        }

        private async Task OpenConnection(Channel<byte[], SendMessage> application, Uri url, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting receive loop");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var stream = await response.Content.ReadAsStreamAsync();
            var pipelineReader = stream.AsPipelineReader(cancellationToken);
            var readCancellationRegistration = cancellationToken.Register(
                reader => ((IPipeReader)reader).CancelPendingRead(), pipelineReader);
            try
            {
                while (true)
                {
                    var result = await pipelineReader.ReadAsync();
                    var input = result.Buffer;
                    if (result.IsCancelled || (input.IsEmpty && result.IsCompleted))
                    {
                        _logger.LogDebug("Server-Sent Event Stream ended");
                        break;
                    }

                    var consumed = input.Start;
                    var examined = input.End;

                    try
                    {
                        var parseResult = _parser.ParseMessage(input, out consumed, out examined, out var buffer);

                        switch (parseResult)
                        {
                            case ServerSentEventsMessageParser.ParseResult.Completed:
                                _application.Out.TryWrite(buffer);
                                _parser.Reset();
                                break;
                            case ServerSentEventsMessageParser.ParseResult.Incomplete:
                                if (result.IsCompleted)
                                {
                                    throw new FormatException("Incomplete message");
                                }
                                break;
                        }
                    }
                    finally
                    {
                        pipelineReader.Advance(consumed, examined);
                    }
                }
            }
            finally
            {
                readCancellationRegistration.Dispose();
                _transportCts.Cancel();
                stream.Dispose();
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Transport {transportName} is stopping", nameof(ServerSentEventsTransport));
            _transportCts.Cancel();
            _application.Out.TryComplete();
            await Running;
        }
    }
}
