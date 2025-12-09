// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal static partial class SendUtils
{
    public static async Task SendMessages(Uri sendUrl, IDuplexPipe application, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken = default)
    {
        Log.SendStarted(logger);

        try
        {
            while (true)
            {
                var result = await application.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
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
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                        request.Content = new ReadOnlySequenceContent(buffer);

                        // ResponseHeadersRead instructs SendAsync to return once headers are read
                        // rather than buffer the entire response. This gives a small perf boost.
                        // Note that it is important to dispose of the response when doing this to
                        // avoid leaving the connection open.
                        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
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

    // AccessTokenHttpMessageHandler relies on this being reusable
    private sealed class ReadOnlySequenceContent : HttpContent
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

    // When adding a new log message make sure to check with LongPollingTransport and ServerSentEventsTransport that share these logs to not have conflicting EventIds
    // We start the IDs at 100 to make it easy to avoid conflicting IDs
    private static partial class Log
    {
        [LoggerMessage(100, LogLevel.Debug, "Starting the send loop.", EventName = "SendStarted")]
        public static partial void SendStarted(ILogger logger);

        [LoggerMessage(102, LogLevel.Debug, "Send loop canceled.", EventName = "SendCanceled")]
        public static partial void SendCanceled(ILogger logger);

        [LoggerMessage(101, LogLevel.Debug, "Send loop stopped.", EventName = "SendStopped")]
        public static partial void SendStopped(ILogger logger);

        [LoggerMessage(103, LogLevel.Debug, "Sending {Count} bytes to the server using url: {Url}.", EventName = "SendingMessages")]
        public static partial void SendingMessages(ILogger logger, long count, Uri url);

        [LoggerMessage(104, LogLevel.Debug, "Message(s) sent successfully.", EventName = "SentSuccessfully")]
        public static partial void SentSuccessfully(ILogger logger);

        [LoggerMessage(105, LogLevel.Debug, "No messages in batch to send.", EventName = "NoMessages")]
        public static partial void NoMessages(ILogger logger);

        [LoggerMessage(106, LogLevel.Error, "Error while sending to '{Url}'.", EventName = "ErrorSending")]
        public static partial void ErrorSending(ILogger logger, Uri url, Exception exception);
    }
}
