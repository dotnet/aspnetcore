// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class MaxRequestBufferSizeTests
    {
        private const int _dataLength = 20 * 1024 * 1024;

        private static readonly string[] _requestLines = new[]
        {
            "POST / HTTP/1.0\r\n",
            $"Content-Length: {_dataLength}\r\n",
            "\r\n"
        };

        public static IEnumerable<object[]> LargeUploadData
        {
            get
            {
                var maxRequestBufferSizeValues = new Tuple<long?, bool>[] {
                    // Smallest buffer that can hold a test request line without causing
                    // the server to hang waiting for the end of the request line or
                    // a header line.
                    Tuple.Create((long?)(_requestLines.Max(line => line.Length)), true),

                    // Small buffer, but large enough to hold all request headers.
                    Tuple.Create((long?)16 * 1024, true),

                    // Default buffer.
                    Tuple.Create((long?)1024 * 1024, true),

                    // Larger than default, but still significantly lower than data, so client should be paused.
                    // On Windows, the client is usually paused around (MaxRequestBufferSize + 700,000).
                    // On Linux, the client is usually paused around (MaxRequestBufferSize + 10,000,000).
                    Tuple.Create((long?)5 * 1024 * 1024, true),

                    // Even though maxRequestBufferSize < _dataLength, client should not be paused since the
                    // OS-level buffers in client and/or server will handle the overflow.
                    Tuple.Create((long?)_dataLength - 1, false),

                    // Buffer is exactly the same size as data.  Exposed race condition where
                    // IConnectionControl.Resume() was called after socket was disconnected.
                    Tuple.Create((long?)_dataLength, false),

                    // Largest possible buffer, should never trigger backpressure.
                    Tuple.Create((long?)long.MaxValue, false),

                    // Disables all code related to computing and limiting the size of the input buffer.
                    Tuple.Create((long?)null, false)
                };
                var sslValues = new[] { true, false };

                return from maxRequestBufferSize in maxRequestBufferSizeValues
                       from ssl in sslValues
                       select new object[] {
                           maxRequestBufferSize.Item1,
                           ssl,
                           maxRequestBufferSize.Item2
                       };
            }
        }

        [Theory]
        [MemberData(nameof(LargeUploadData))]
        public async Task LargeUpload(long? maxRequestBufferSize, bool connectionAdapter, bool expectPause)
        {
            // Parameters
            var data = new byte[_dataLength];
            var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
            var bytesWrittenPollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);
            var maxSendSize = 4096;

            // Initialize data with random bytes
            (new Random()).NextBytes(data);

            var startReadingRequestBody = new TaskCompletionSource<object>();
            var clientFinishedSendingRequestBody = new TaskCompletionSource<object>();
            var lastBytesWritten = DateTime.MaxValue;

            using (var host = StartWebHost(maxRequestBufferSize, data, connectionAdapter, startReadingRequestBody, clientFinishedSendingRequestBody))
            {
                var port = host.GetPort();
                using (var socket = CreateSocket(port))
                using (var stream = new NetworkStream(socket))
                {
                    await WritePostRequestHeaders(stream, data.Length);

                    var bytesWritten = 0;

                    Func<Task> sendFunc = async () =>
                    {
                        while (bytesWritten < data.Length)
                        {
                            var size = Math.Min(data.Length - bytesWritten, maxSendSize);
                            await stream.WriteAsync(data, bytesWritten, size).ConfigureAwait(false);
                            bytesWritten += size;
                            lastBytesWritten = DateTime.Now;
                        }

                        Assert.Equal(data.Length, bytesWritten);
                        clientFinishedSendingRequestBody.TrySetResult(null);
                    };

                    var sendTask = sendFunc();

                    if (expectPause)
                    {
                        // The minimum is (maxRequestBufferSize - maxSendSize + 1), since if bytesWritten is
                        // (maxRequestBufferSize - maxSendSize) or smaller, the client should be able to
                        // complete another send.
                        var minimumExpectedBytesWritten = maxRequestBufferSize.Value - maxSendSize + 1;

                        // The maximum is harder to determine, since there can be OS-level buffers in both the client
                        // and server, which allow the client to send more than maxRequestBufferSize before getting
                        // paused.  We assume the combined buffers are smaller than the difference between
                        // data.Length and maxRequestBufferSize.
                        var maximumExpectedBytesWritten = data.Length - 1;

                        // Block until the send task has gone a while without writing bytes AND
                        // the bytes written exceeds the minimum expected.  This indicates the server buffer
                        // is full.
                        //
                        // If the send task is paused before the expected number of bytes have been
                        // written, keep waiting since the pause may have been caused by something else
                        // like a slow machine.
                        while ((DateTime.Now - lastBytesWritten) < bytesWrittenTimeout ||
                               bytesWritten < minimumExpectedBytesWritten)
                        {
                            await Task.Delay(bytesWrittenPollingInterval);
                        }

                        // Verify the number of bytes written before the client was paused.
                        Assert.InRange(bytesWritten, minimumExpectedBytesWritten, maximumExpectedBytesWritten);

                        // Tell server to start reading request body
                        startReadingRequestBody.TrySetResult(null);

                        // Wait for sendTask to finish sending the remaining bytes
                        await sendTask;
                    }
                    else
                    {
                        // Ensure all bytes can be sent before the server starts reading
                        await sendTask;

                        // Tell server to start reading request body
                        startReadingRequestBody.TrySetResult(null);
                    }

                    using (var reader = new StreamReader(stream, Encoding.ASCII))
                    {
                        var response = reader.ReadToEnd();
                        Assert.Contains($"bytesRead: {data.Length}", response);
                    }
                }
            }
        }

        [Fact]
        public async Task ServerShutsDownGracefullyWhenMaxRequestBufferSizeExceeded()
        {
            // Parameters
            var data = new byte[_dataLength];
            var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
            var bytesWrittenPollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);
            var maxSendSize = 4096;

            var startReadingRequestBody = new TaskCompletionSource<object>();
            var clientFinishedSendingRequestBody = new TaskCompletionSource<object>();
            var lastBytesWritten = DateTime.MaxValue;

            using (var host = StartWebHost(16 * 1024, data, false, startReadingRequestBody, clientFinishedSendingRequestBody))
            {
                var port = host.GetPort();
                using (var socket = CreateSocket(port))
                using (var stream = new NetworkStream(socket))
                {
                    await WritePostRequestHeaders(stream, data.Length);

                    var bytesWritten = 0;

                    Func<Task> sendFunc = async () =>
                    {
                        while (bytesWritten < data.Length)
                        {
                            var size = Math.Min(data.Length - bytesWritten, maxSendSize);
                            await stream.WriteAsync(data, bytesWritten, size).ConfigureAwait(false);
                            bytesWritten += size;
                            lastBytesWritten = DateTime.Now;
                        }

                        clientFinishedSendingRequestBody.TrySetResult(null);
                    };

                    var ignore = sendFunc();

                    // The minimum is (maxRequestBufferSize - maxSendSize + 1), since if bytesWritten is
                    // (maxRequestBufferSize - maxSendSize) or smaller, the client should be able to
                    // complete another send.
                    var minimumExpectedBytesWritten = (16 * 1024) - maxSendSize + 1;

                    // The maximum is harder to determine, since there can be OS-level buffers in both the client
                    // and server, which allow the client to send more than maxRequestBufferSize before getting
                    // paused.  We assume the combined buffers are smaller than the difference between
                    // data.Length and maxRequestBufferSize.
                    var maximumExpectedBytesWritten = data.Length - 1;

                    // Block until the send task has gone a while without writing bytes AND
                    // the bytes written exceeds the minimum expected.  This indicates the server buffer
                    // is full.
                    //
                    // If the send task is paused before the expected number of bytes have been
                    // written, keep waiting since the pause may have been caused by something else
                    // like a slow machine.
                    while ((DateTime.Now - lastBytesWritten) < bytesWrittenTimeout ||
                           bytesWritten < minimumExpectedBytesWritten)
                    {
                        await Task.Delay(bytesWrittenPollingInterval);
                    }

                    // Verify the number of bytes written before the client was paused.
                    Assert.InRange(bytesWritten, minimumExpectedBytesWritten, maximumExpectedBytesWritten);

                    // Dispose host prior to closing connection to verify the server doesn't throw during shutdown
                    // if a connection no longer has alloc and read callbacks configured.
                    host.Dispose();
                }
            }
        }

        private static IWebHost StartWebHost(long? maxRequestBufferSize, 
            byte[] expectedBody, 
            bool useConnectionAdapter, 
            TaskCompletionSource<object> startReadingRequestBody,
            TaskCompletionSource<object> clientFinishedSendingRequestBody)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        if (useConnectionAdapter)
                        {
                            listenOptions.ConnectionAdapters.Add(new PassThroughConnectionAdapter());
                        }
                    });

                    options.Limits.MaxRequestBufferSize = maxRequestBufferSize;

                    if (maxRequestBufferSize.HasValue &&
                        maxRequestBufferSize.Value < options.Limits.MaxRequestLineSize)
                    {
                        options.Limits.MaxRequestLineSize = (int)maxRequestBufferSize;
                    }

                    if (maxRequestBufferSize.HasValue &&
                        maxRequestBufferSize.Value < options.Limits.MaxRequestHeadersTotalSize)
                    {
                        options.Limits.MaxRequestHeadersTotalSize = (int)maxRequestBufferSize;
                    }
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Configure(app => app.Run(async context =>
                {
                    await startReadingRequestBody.Task.TimeoutAfter(TimeSpan.FromSeconds(30));

                    var buffer = new byte[expectedBody.Length];
                    var bytesRead = 0;
                    while (bytesRead < buffer.Length)
                    {
                        bytesRead += await context.Request.Body.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                    }

                    await clientFinishedSendingRequestBody.Task.TimeoutAfter(TimeSpan.FromSeconds(120));

                    // Verify client didn't send extra bytes
                    if (context.Request.Body.ReadByte() != -1)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Client sent more bytes than expectedBody.Length");
                        return;
                    }

                    // Verify bytes received match expectedBody
                    for (int i = 0; i < expectedBody.Length; i++)
                    {
                        if (buffer[i] != expectedBody[i])
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync($"Bytes received do not match expectedBody at position {i}");
                            return;
                        }
                    }

                    await context.Response.WriteAsync($"bytesRead: {bytesRead.ToString()}");
                }))
                .Build();

            host.Start();

            return host;
        }

        private static Socket CreateSocket(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Timeouts large enough to prevent false positives, but small enough to fail quickly.
            socket.SendTimeout = 10 * 1000;
            socket.ReceiveTimeout = 120 * 1000;

            socket.Connect(IPAddress.Loopback, port);

            return socket;
        }

        private static async Task WritePostRequestHeaders(Stream stream, int contentLength)
        {
            using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
            {
                foreach (var line in _requestLines)
                {
                    await writer.WriteAsync(line).ConfigureAwait(false);
                }
            }
        }
    }
}
