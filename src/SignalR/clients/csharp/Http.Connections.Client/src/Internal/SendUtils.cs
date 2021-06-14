// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal static class SendUtils
    {
        public static async Task SendMessages(Uri sendUrl, IDuplexPipe application, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken = default)
        {
            Log.SendStarted(logger);

            try
            {
                while (true)
                {
                    var result = await application.Input.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            Log.SendCanceled(logger);
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            Log.SendingMessages(logger, buffer.Length, sendUrl);

                            // Send them in a single post
                            var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                            // Corefx changed the default version and High Sierra curlhandler tries to upgrade request
                            request.Version = new Version(1, 1);

                            request.Content = new ReadOnlySequenceContent(buffer);

                            // ResponseHeadersRead instructs SendAsync to return once headers are read
                            // rather than buffer the entire response. This gives a small perf boost.
                            // Note that it is important to dispose of the response when doing this to
                            // avoid leaving the connection open.
                            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                            {
                                response.EnsureSuccessStatusCode();
                            }

                            Log.SentSuccessfully(logger);
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                        else
                        {
                            Log.NoMessages(logger);
                        }
                    }
                    finally
                    {
                        application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.SendCanceled(logger);
            }
            catch (Exception ex)
            {
                Log.ErrorSending(logger, sendUrl, ex);
                throw;
            }
            finally
            {
                application.Input.Complete();
            }

            Log.SendStopped(logger);
        }

        private class ReadOnlySequenceContent : HttpContent
        {
            private readonly ReadOnlySequence<byte> _buffer;

            public ReadOnlySequenceContent(in ReadOnlySequence<byte> buffer)
            {
                _buffer = buffer;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                return stream.WriteAsync(_buffer).AsTask();
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _buffer.Length;
                return true;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _sendStarted =
                LoggerMessage.Define(LogLevel.Debug, new EventId(100, "SendStarted"), "Starting the send loop.");

            private static readonly Action<ILogger, Exception?> _sendStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(101, "SendStopped"), "Send loop stopped.");

            private static readonly Action<ILogger, Exception?> _sendCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(102, "SendCanceled"), "Send loop canceled.");

            private static readonly Action<ILogger, long, Uri, Exception?> _sendingMessages =
                LoggerMessage.Define<long, Uri>(LogLevel.Debug, new EventId(103, "SendingMessages"), "Sending {Count} bytes to the server using url: {Url}.");

            private static readonly Action<ILogger, Exception?> _sentSuccessfully =
                LoggerMessage.Define(LogLevel.Debug, new EventId(104, "SentSuccessfully"), "Message(s) sent successfully.");

            private static readonly Action<ILogger, Exception?> _noMessages =
                LoggerMessage.Define(LogLevel.Debug, new EventId(105, "NoMessages"), "No messages in batch to send.");

            private static readonly Action<ILogger, Uri, Exception> _errorSending =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(106, "ErrorSending"), "Error while sending to '{Url}'.");

            // When adding a new log message make sure to check with LongPollingTransport and ServerSentEventsTransport that share these logs to not have conflicting EventIds
            // We start the IDs at 100 to make it easy to avoid conflicting IDs

            public static void SendStarted(ILogger logger)
            {
                _sendStarted(logger, null);
            }

            public static void SendCanceled(ILogger logger)
            {
                _sendCanceled(logger, null);
            }

            public static void SendStopped(ILogger logger)
            {
                _sendStopped(logger, null);
            }

            public static void SendingMessages(ILogger logger, long count, Uri url)
            {
                _sendingMessages(logger, count, url, null);
            }

            public static void SentSuccessfully(ILogger logger)
            {
                _sentSuccessfully(logger, null);
            }

            public static void NoMessages(ILogger logger)
            {
                _noMessages(logger, null);
            }

            public static void ErrorSending(ILogger logger, Uri url, Exception exception)
            {
                _errorSending(logger, url, exception);
            }
        }
    }
}
