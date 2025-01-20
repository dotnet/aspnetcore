// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

public class MaxRequestBufferSizeTests : LoggedTest
{
    // The client is typically paused after uploading this many bytes:
    //
    // OS                   MaxRequestBufferSize (MB)   connectionAdapter   Transport   min pause (MB)      max pause (MB)
    // ---------------      -------------------------   -----------------   ---------   --------------      --------------
    // Windows 10 1803      1                           false               Libuv       1.7                 3.3
    // Windows 10 1803      1                           false               Sockets     1.7                 4.4
    // Windows 10 1803      1                           true                Libuv       3.0                 8.4
    // Windows 10 1803      1                           true                Sockets     3.2                 9.0
    //
    // Windows 10 1803      5                           false               Libuv       6                   13
    // Windows 10 1803      5                           false               Sockets     7                   24
    // Windows 10 1803      5                           true                Libuv       12                  12
    // Windows 10 1803      5                           true                Sockets     12                  36
    // Ubuntu 18.04         5                           false               Libuv       13                  15
    // Ubuntu 18.04         5                           false               Sockets     13                  15
    // Ubuntu 18.04         5                           true                Libuv       19                  20
    // Ubuntu 18.04         5                           true                Sockets     18                  20
    // macOS 10.13.4        5                           false               Libuv       6                   6
    // macOS 10.13.4        5                           false               Sockets     6                   6
    // macOS 10.13.4        5                           true                Libuv       11                  11
    // macOS 10.13.4        5                           true                Sockets     11                  11
    //
    // When connectionAdapter=true, the MaxRequestBufferSize is set on two pipes, so it's effectively doubled.
    //
    // To ensure reliability, _dataLength must be greater than the largest "max pause" in any configuration
    private const int _dataLength = 100 * 1024 * 1024;

    private static readonly string[] _requestLines = new[]
    {
        "POST / HTTP/1.1\r\n",
        "Host: \r\n",
        $"Content-Length: {_dataLength}\r\n",
        "\r\n"
    };

    public static IEnumerable<object[]> LargeUploadData
    {
        get
        {
            var totalHeaderSize = 0;

            for (var i = 1; i < _requestLines.Length - 1; i++)
            {
                totalHeaderSize += _requestLines[i].Length;
            }

            var maxRequestBufferSizeValues = new Tuple<long?, bool>[] {
                // Smallest buffer that can hold the test request headers without causing
                // the server to hang waiting for the end of the request line or
                // a header line.
                Tuple.Create((long?)totalHeaderSize, true),

                // Small buffer, but large enough to hold all request headers.
                Tuple.Create((long?)16 * 1024, true),

                // Default buffer.
                Tuple.Create((long?)1024 * 1024, true),

                // Larger than default, but still significantly lower than data, so client should be paused.
                Tuple.Create((long?)5 * 1024 * 1024, true),

                // Even though maxRequestBufferSize < _dataLength, client should not be paused since the
                // OS-level buffers in client and/or server will handle the overflow.
                Tuple.Create((long?)_dataLength - 1, false),

                // Buffer is exactly the same size as data.  Exposed race condition where
                // the connection was resumed after socket was disconnected.
                Tuple.Create((long?)_dataLength, false),

                // Largest possible buffer, should never trigger backpressure.
                Tuple.Create((long?)long.MaxValue, false),

                // Disables all code related to computing and limiting the size of the input buffer.
                Tuple.Create((long?)null, false)
            };
            var sslValues = new[] { true, false };

            return from maxRequestBufferSize in maxRequestBufferSizeValues
                   from ssl in sslValues
                   select new object[]
                   {
                        maxRequestBufferSize.Item1,
                        ssl,
                        maxRequestBufferSize.Item2
                    };
        }
    }

    // On helix retry list - inherently flaky (trying to manipulate the state of the server's buffer)
    [Theory]
    [MemberData(nameof(LargeUploadData))]
    public async Task LargeUpload(long? maxRequestBufferSize, bool connectionAdapter, bool expectPause)
    {
        // Parameters
        var data = new byte[_dataLength];
        var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
        var bytesWrittenPollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);
        var maxSendSize = 4096;

        var startReadingRequestBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientFinishedSendingRequestBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lastBytesWritten = DateTime.MaxValue;

        var memoryPoolFactory = new DiagnosticMemoryPoolFactory(allowLateReturn: true);

        using (var host = await StartHost(maxRequestBufferSize, data, connectionAdapter, startReadingRequestBody, clientFinishedSendingRequestBody, memoryPoolFactory.Create))
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
                    clientFinishedSendingRequestBody.TrySetResult();
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
                    startReadingRequestBody.TrySetResult();

                    // Wait for sendTask to finish sending the remaining bytes
                    await sendTask;
                }
                else
                {
                    // Ensure all bytes can be sent before the server starts reading
                    await sendTask;

                    // Tell server to start reading request body
                    startReadingRequestBody.TrySetResult();
                }

                await AssertStreamContains(stream, $"bytesRead: {data.Length}");
            }
            await host.StopAsync();
        }

        await memoryPoolFactory.WhenAllBlocksReturned(TestConstants.DefaultTimeout);
    }

    [Fact]
    public async Task ServerShutsDownGracefullyWhenMaxRequestBufferSizeExceeded()
    {
        // Parameters
        var data = new byte[_dataLength];
        var bytesWrittenTimeout = TimeSpan.FromMilliseconds(100);
        var bytesWrittenPollingInterval = TimeSpan.FromMilliseconds(bytesWrittenTimeout.TotalMilliseconds / 10);
        var maxSendSize = 4096;

        var startReadingRequestBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientFinishedSendingRequestBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lastBytesWritten = DateTime.MaxValue;

        var memoryPoolFactory = new DiagnosticMemoryPoolFactory(allowLateReturn: true);

        using (var host = await StartHost(16 * 1024, data, false, startReadingRequestBody, clientFinishedSendingRequestBody, memoryPoolFactory.Create))
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

                    clientFinishedSendingRequestBody.TrySetResult();
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
                await host.StopAsync();
                host.Dispose();
            }
        }
        // Allow appfunc to unblock
        startReadingRequestBody.SetResult();
        clientFinishedSendingRequestBody.SetResult();

        try
        {
            await memoryPoolFactory.WhenAllBlocksReturned(TestConstants.DefaultTimeout);
        }
        catch (AggregateException)
        {
            // This test is inherently racey. The server could try to use blocks that have been disposed.
            // Ignore errors related to this:
            //
            // System.AggregateException : Exceptions occurred while accessing blocks(Block is backed by disposed slab)
            // ---- System.InvalidOperationException : Block is backed by disposed slab
        }
    }

    private async Task<IHost> StartHost(long? maxRequestBufferSize,
        byte[] expectedBody,
        bool useConnectionAdapter,
        TaskCompletionSource startReadingRequestBody,
        TaskCompletionSource clientFinishedSendingRequestBody,
        Func<MemoryPool<byte>> memoryPoolFactory = null)
    {
        var host = TransportSelector.GetHostBuilder(memoryPoolFactory, maxRequestBufferSize)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                        {
                            if (useConnectionAdapter)
                            {
                                listenOptions.UsePassThrough();
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

                        options.Limits.MinRequestBodyDataRate = null;

                        options.Limits.MaxRequestBodySize = _dataLength;
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .Configure(app => app.Run(async context =>
                    {
                        await startReadingRequestBody.Task.TimeoutAfter(TimeSpan.FromSeconds(120));

                        var buffer = new byte[expectedBody.Length];
                        var bytesRead = 0;
                        while (bytesRead < buffer.Length)
                        {
                            bytesRead += await context.Request.Body.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                        }

                        await clientFinishedSendingRequestBody.Task.TimeoutAfter(TimeSpan.FromSeconds(120));

                        // Verify client didn't send extra bytes
                        if (await context.Request.Body.ReadAsync(new byte[1], 0, 1) != 0)
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Client sent more bytes than expectedBody.Length");
                            return;
                        }

                        await context.Response.WriteAsync($"bytesRead: {bytesRead}");
                    }));
            })
            .ConfigureServices(AddTestLogging)
            .Build();

        await host.StartAsync();

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

    // THIS IS NOT GENERAL PURPOSE. If the initial characters could repeat, this is broken. However, since we're
    // looking for /bytesWritten: \d+/ and the initial "b" cannot occur elsewhere in the pattern, this works.
    private static async Task AssertStreamContains(Stream stream, string expectedSubstring)
    {
        var expectedBytes = Encoding.ASCII.GetBytes(expectedSubstring);
        var exptectedLength = expectedBytes.Length;
        var responseBuffer = new byte[exptectedLength];

        var matchedChars = 0;

        while (matchedChars < exptectedLength)
        {
            var count = await stream.ReadAsync(responseBuffer, 0, exptectedLength - matchedChars).DefaultTimeout();

            if (count == 0)
            {
                Assert.Fail("Stream completed without expected substring.");
            }

            for (var i = 0; i < count && matchedChars < exptectedLength; i++)
            {
                if (responseBuffer[i] == expectedBytes[matchedChars])
                {
                    matchedChars++;
                }
                else
                {
                    matchedChars = 0;
                }
            }
        }
    }
}
