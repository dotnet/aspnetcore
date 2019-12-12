// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Interop.FunctionalTests
{
    /// <summary>
    /// This tests interop with System.Net.Http.HttpClient (SocketHttpHandler) using HTTP/2 (H2 and H2C)
    /// </summary>
    public class HttpClientHttp2InteropTests : LoggedTest
    {
        public HttpClientHttp2InteropTests()
        {
            // H2C
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public static IEnumerable<object[]> SupportedSchemes
        {
            get
            {
                var list = new List<object[]>()
                {
                    new[] { "http" }
                };

                if (Utilities.CurrentPlatformSupportsAlpn())
                {
                    list.Add(new[] { "https" });
                }

                return list;
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task HelloWorld(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context => context.Response.WriteAsync("Hello World")));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url).DefaultTimeout();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Echo(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await context.Request.BodyReader.CopyToAsync(context.Response.BodyWriter).DefaultTimeout();
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = true;

            using var request = CreateRequestMessage(HttpMethod.Post, url, new BulkContent());
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();

            Assert.Equal(HttpVersion.Version20, response.Version);
            await BulkContent.VerifyContent(await response.Content.ReadAsStreamAsync().DefaultTimeout());
            await host.StopAsync().DefaultTimeout();
        }

        // Concurrency testing
        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task MultiplexGet(string scheme)
        {
            var requestsReceived = 0;
            var requestCount = 10;
            var allRequestsReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        if (Interlocked.Increment(ref requestsReceived) == requestCount)
                        {
                            allRequestsReceived.SetResult(0);
                        }
                        await allRequestsReceived.Task;
                        var content = new BulkContent();
                        await content.CopyToAsync(context.Response.Body).DefaultTimeout();
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var requestTasks = new List<Task>(requestCount);
            for (var i = 0; i < requestCount; i++)
            {
                requestTasks.Add(RunRequest(url));
            }

            async Task RunRequest(string url)
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();

                Assert.Equal(HttpVersion.Version20, response.Version);
                await BulkContent.VerifyContent(await response.Content.ReadAsStreamAsync()).DefaultTimeout();
            };

            await Task.WhenAll(requestTasks);
            await host.StopAsync().DefaultTimeout();
        }

        // Concurrency testing
        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task MultiplexEcho(string scheme)
        {
            var requestsReceived = 0;
            var requestCount = 10;
            var allRequestsReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        if (Interlocked.Increment(ref requestsReceived) == requestCount)
                        {
                            allRequestsReceived.SetResult(0);
                        }
                        await allRequestsReceived.Task;
                        await context.Request.BodyReader.CopyToAsync(context.Response.BodyWriter).DefaultTimeout();
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = true;

            var requestTasks = new List<Task>(requestCount);
            for (var i = 0; i < requestCount; i++)
            {
                requestTasks.Add(RunRequest(url));
            }

            async Task RunRequest(string url)
            {
                using var request = CreateRequestMessage(HttpMethod.Post, url, new BulkContent());
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();

                Assert.Equal(HttpVersion.Version20, response.Version);
                await BulkContent.VerifyContent(await response.Content.ReadAsStreamAsync().DefaultTimeout());
            };

            await Task.WhenAll(requestTasks);
            await host.StopAsync().DefaultTimeout();
        }

        private class BulkContent : HttpContent
        {
            private static readonly byte[] Content;
            private static readonly int Repititions = 200;

            static BulkContent()
            {
                Content = new byte[999]; // Intentionally not matching normal memory page sizes to ensure we stress boundaries.
                for (var i = 0; i < Content.Length; i++)
                {
                    Content[i] = (byte)i;
                }
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                for (var i = 0; i < Repititions; i++)
                {
                    using (var timer = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        await stream.WriteAsync(Content, 0, Content.Length, timer.Token).DefaultTimeout();
                    }
                    await Task.Yield(); // Intermix writes
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            public static async Task VerifyContent(Stream stream)
            {
                byte[] buffer = new byte[1024];
                var totalRead = 0;
                var patternOffset = 0;
                int read = 0;
                using (var timer = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    read = await stream.ReadAsync(buffer, 0, buffer.Length, timer.Token).DefaultTimeout();
                }

                while (read > 0)
                {
                    totalRead += read;
                    Assert.True(totalRead <= Repititions * Content.Length, "Too Long");

                    for (var offset = 0; offset < read; offset++)
                    {
                        Assert.Equal(Content[patternOffset % Content.Length], buffer[offset]);
                        patternOffset++;
                    }

                    using var timer = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    read = await stream.ReadAsync(buffer, 0, buffer.Length, timer.Token).DefaultTimeout();
                }

                Assert.True(totalRead == Repititions * Content.Length, "Too Short");
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task BidirectionalStreaming(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        var reader = context.Request.BodyReader;
                        // Read Hello World and echo it back to the client, twice
                        for (var i = 0; i < 2; i++)
                        {
                            var readResult = await reader.ReadAsync().DefaultTimeout();
                            while (!readResult.IsCompleted && readResult.Buffer.Length < "Hello World".Length)
                            {
                                reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                                readResult = await reader.ReadAsync().DefaultTimeout();
                            }

                            var sequence = readResult.Buffer.Slice(0, "Hello World".Length);
                            Assert.True(sequence.IsSingleSegment);
                            await context.Response.BodyWriter.WriteAsync(sequence.First).DefaultTimeout();
                            reader.AdvanceTo(sequence.End);
                        }

                        var finalResult = await reader.ReadAsync().DefaultTimeout();
                        Assert.True(finalResult.IsCompleted && finalResult.Buffer.Length == 0);
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = true;

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            // The server won't send headers until it gets the first message
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            var response = await responseTask;

            Assert.Equal(HttpVersion.Version20, response.Version);
            var stream = await response.Content.ReadAsStreamAsync().DefaultTimeout();
            await ReadStreamHelloWorld(stream);

            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            streamingContent.Complete();

            await ReadStreamHelloWorld(stream);
            Assert.Equal(0, await stream.ReadAsync(new byte[10], 0, 10).DefaultTimeout());
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task BidirectionalStreamingMoreClientData(string scheme)
        {
            var lastPacket = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        var reader = context.Request.BodyReader;

                        var readResult = await reader.ReadAsync().DefaultTimeout();
                        while (!readResult.IsCompleted && readResult.Buffer.Length < "Hello World".Length)
                        {
                            reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                            readResult = await reader.ReadAsync().DefaultTimeout();
                        }

                        var sequence = readResult.Buffer.Slice(0, "Hello World".Length);
                        Assert.True(sequence.IsSingleSegment);
                        await context.Response.BodyWriter.WriteAsync(sequence.First).DefaultTimeout();
                        reader.AdvanceTo(sequence.End);
                        await context.Response.CompleteAsync().DefaultTimeout();

                        try
                        {
                            // The client sends one more packet after the server completes
                            readResult = await reader.ReadAsync().DefaultTimeout();
                            while (!readResult.IsCompleted && readResult.Buffer.Length < "Hello World".Length)
                            {
                                reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                                readResult = await reader.ReadAsync().DefaultTimeout();
                            }

                            Assert.True(readResult.Buffer.IsSingleSegment);
                            var result = Encoding.UTF8.GetString(readResult.Buffer.FirstSpan);
                            reader.AdvanceTo(readResult.Buffer.End);

                            var finalResult = await reader.ReadAsync().DefaultTimeout();
                            Assert.True(finalResult.IsCompleted && finalResult.Buffer.Length == 0);
                            lastPacket.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            lastPacket.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = true;

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            // The server doesn't respond until we send the first set of data
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            var response = await responseTask;

            Assert.Equal(HttpVersion.Version20, response.Version);
            var stream = await response.Content.ReadAsStreamAsync();
            await ReadStreamHelloWorld(stream);

            Assert.Equal(0, await stream.ReadAsync(new byte[10], 0, 10).DefaultTimeout());
            stream.Dispose();

            // Send one more message after the server has finished.
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            streamingContent.Complete();

            var lastData = await lastPacket.Task.DefaultTimeout();
            Assert.Equal("Hello World", lastData);

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ReverseEcho(string scheme)
        {
            var clientEcho = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("Hello World");
                        await context.Response.CompleteAsync().DefaultTimeout();

                        try
                        {
                            using var streamReader = new StreamReader(context.Request.Body);
                            var read = await streamReader.ReadToEndAsync().DefaultTimeout();
                            clientEcho.SetResult(read);
                        }
                        catch (Exception ex)
                        {
                            clientEcho.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();

            Assert.Equal(HttpVersion.Version20, response.Version);

            // Read Hello World and echo it back to the server.
            var read = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello World", read);

            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            streamingContent.Complete();

            Assert.Equal("Hello World", await clientEcho.Task.DefaultTimeout());
            await host.StopAsync().DefaultTimeout();
        }

        private class StreamingContent : HttpContent
        {
            private TaskCompletionSource<int> _sendStarted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            private Func<string, Task> _sendContent;
            private TaskCompletionSource<int> _sendComplete;

            public StreamingContent()
            {
            }

            public async Task SendAsync(string text)
            {
                await _sendStarted.Task;
                await _sendContent(text);
            }

            public void Complete()
            {
                if (_sendComplete == null)
                {
                    throw new InvalidOperationException("Sending hasn't started yet.");
                }
                _sendComplete.TrySetResult(0);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _sendComplete = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _sendContent = async text =>
                {
                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(text);
                        await stream.WriteAsync(bytes);
                        await stream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        _sendComplete.TrySetException(ex);
                        throw;
                    }
                };
                _sendStarted.SetResult(0);
                return _sendComplete.Task;
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ResponseTrailersWithoutData(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        context.Response.DeclareTrailer("TestTrailer");
                        context.Response.AppendTrailer("TestTrailer", "TestValue");
                        return Task.CompletedTask;
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url).DefaultTimeout();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("TestTrailer", response.Headers.Trailer.Single());
            // The response is buffered, we must already have the trailers.
            Assert.Equal("TestValue", response.TrailingHeaders.GetValues("TestTrailer").Single());
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ResponseTrailersWithData(string scheme)
        {
            var headersReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        context.Response.DeclareTrailer("TestTrailer");
                        await context.Response.WriteAsync("Hello ");
                        await headersReceived.Task.DefaultTimeout();
                        await context.Response.WriteAsync("World");
                        context.Response.AppendTrailer("TestTrailer", "TestValue");
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("TestTrailer", response.Headers.Trailer.Single());
            // The server has not sent trailers yet.
            Assert.False(response.TrailingHeaders.TryGetValues("TestTrailer", out var none));
            headersReceived.SetResult(0);
            var responseBody = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello World", responseBody);
            // The response is buffered, we must already have the trailers.
            Assert.Equal("TestValue", response.TrailingHeaders.GetValues("TestTrailer").Single());
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_BeforeResponse_ClientThrows(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                        return Task.CompletedTask;
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)).DefaultTimeout();
            Assert.Equal("The HTTP/2 server reset the stream. HTTP/2 error code 'CANCEL' (0x8).", exception?.InnerException?.InnerException.Message);
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_AfterHeaders_ClientBodyThrows(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await context.Response.BodyWriter.FlushAsync();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync()).DefaultTimeout();
            Assert.Equal("The HTTP/2 server reset the stream. HTTP/2 error code 'CANCEL' (0x8).", exception?.InnerException?.InnerException.Message);
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_AfterEndStream_NoError(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Hello World");
                        await context.Response.CompleteAsync();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello World", body);
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_AfterTrailers_NoError(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        context.Response.DeclareTrailer("TestTrailer");
                        await context.Response.WriteAsync("Hello World");
                        context.Response.AppendTrailer("TestTrailer", "TestValue");
                        await context.Response.CompleteAsync();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("TestTrailer", response.Headers.Trailer.Single());
            var responseBody = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("Hello World", responseBody);
            // The response is buffered, we must already have the trailers.
            Assert.Equal("TestValue", response.TrailingHeaders.GetValues("TestTrailer").Single());
            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_BeforeRequestBody_ClientBodyThrows(string scheme)
        {
            var clientEcho = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverReset = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var headersReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.BodyWriter.FlushAsync();
                        await headersReceived.Task.DefaultTimeout();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                        serverReset.SetResult(0);

                        try
                        {
                            using var streamReader = new StreamReader(context.Request.Body);
                            var read = await streamReader.ReadToEndAsync().DefaultTimeout();
                            clientEcho.SetResult(read);
                        }
                        catch (Exception ex)
                        {
                            clientEcho.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            headersReceived.SetResult(0);

            Assert.Equal(HttpVersion.Version20, response.Version);

            await serverReset.Task.DefaultTimeout();
            var responseEx = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync().DefaultTimeout());
            Assert.Contains("The HTTP/2 server reset the stream. HTTP/2 error code 'CANCEL' (0x8)", responseEx.ToString());
            await Assert.ThrowsAsync<TaskCanceledException>(() => streamingContent.SendAsync("Hello World").DefaultTimeout());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientEcho.Task.DefaultTimeout());

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_BeforeRequestBodyEnd_ClientBodyThrows(string scheme)
        {
            var clientEcho = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverReset = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var responseHeadersReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        var count = await context.Request.Body.ReadAsync(new byte[11], 0, 11);
                        Assert.Equal(11, count);

                        context.Response.ContentType = "text/plain";
                        await context.Response.BodyWriter.FlushAsync();
                        await responseHeadersReceived.Task.DefaultTimeout();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                        serverReset.SetResult(0);

                        try
                        {
                            using var streamReader = new StreamReader(context.Request.Body);
                            var read = await streamReader.ReadToEndAsync().DefaultTimeout();
                            clientEcho.SetResult(read);
                        }
                        catch (Exception ex)
                        {
                            clientEcho.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var requestTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            await streamingContent.SendAsync("Hello World");
            using var response = await requestTask;
            responseHeadersReceived.SetResult(0);

            Assert.Equal(HttpVersion.Version20, response.Version);

            await serverReset.Task.DefaultTimeout();
            var responseEx = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync().DefaultTimeout());
            Assert.Contains("The HTTP/2 server reset the stream. HTTP/2 error code 'CANCEL' (0x8)", responseEx.ToString());
            await Assert.ThrowsAsync<TaskCanceledException>(() => streamingContent.SendAsync("Hello World").DefaultTimeout());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientEcho.Task.DefaultTimeout());

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ClientReset_BeforeRequestData_ReadThrows(string scheme)
        {
            var requestReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            var readTask = context.Request.Body.ReadAsync(new byte[11], 0, 11);
                            requestReceived.SetResult(0);
                            var ex = await Assert.ThrowsAsync<IOException>(() => readTask).DefaultTimeout();
                            serverResult.SetResult(0);
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var requestCancellation = new CancellationTokenSource();
            var requestTask = client.SendAsync(request, requestCancellation.Token);
            await requestReceived.Task.DefaultTimeout();
            requestCancellation.Cancel();
            await serverResult.Task.DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ClientReset_BeforeRequestDataEnd_ReadThrows(string scheme)
        {
            var requestReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            var read = await context.Request.Body.ReadAsync(new byte[11], 0, 11);
                            Assert.Equal(11, read);
                            var readTask = context.Request.Body.ReadAsync(new byte[11], 0, 11);
                            requestReceived.SetResult(0);
                            var ex = await Assert.ThrowsAsync<IOException>(() => readTask).DefaultTimeout();
                            serverResult.SetResult(0);
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var requestCancellation = new CancellationTokenSource();
            var requestTask = client.SendAsync(request, requestCancellation.Token);
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            await requestReceived.Task.DefaultTimeout();
            requestCancellation.Cancel();
            await serverResult.Task.DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ClientReset_BeforeResponse_ResponseSuppressed(string scheme)
        {
            var requestReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            context.RequestAborted.Register(() => serverResult.SetResult(0));
                            requestReceived.SetResult(0);
                            await serverResult.Task.DefaultTimeout();
                            await context.Response.WriteAsync("Hello World").DefaultTimeout();
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var requestCancellation = new CancellationTokenSource();
            var requestTask = client.GetAsync(url, requestCancellation.Token);
            await requestReceived.Task.DefaultTimeout();
            requestCancellation.Cancel();
            await serverResult.Task.DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ClientReset_BeforeEndStream_WritesSuppressed(string scheme)
        {
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            context.RequestAborted.Register(() => serverResult.SetResult(0));
                            await context.Response.WriteAsync("Hello World").DefaultTimeout();
                            await serverResult.Task.DefaultTimeout();
                            await context.Response.WriteAsync("Hello World").DefaultTimeout();
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();
            var read = await responseStream.ReadAsync(new byte[11], 0, 11).DefaultTimeout();
            Assert.Equal(11, read);
            responseStream.Dispose(); // Sends reset
            await serverResult.Task.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ClientReset_BeforeTrailers_TrailersSuppressed(string scheme)
        {
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            context.RequestAborted.Register(() => serverResult.SetResult(0));
                            await context.Response.WriteAsync("Hello World").DefaultTimeout();
                            await serverResult.Task.DefaultTimeout();
                            context.Response.AppendTrailer("foo", "bar");
                            await context.Response.CompleteAsync();
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            var responseStream = await response.Content.ReadAsStreamAsync().DefaultTimeout();
            var read = await responseStream.ReadAsync(new byte[11], 0, 11).DefaultTimeout();
            Assert.Equal(11, read);
            responseStream.Dispose(); // Sends reset
            await serverResult.Task.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task RequestHeaders_MultipleFrames_Accepted(string scheme)
        {
            var oneKbString = new string('a', 1024);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        try
                        {
                            for (var i = 0; i < 20; i++)
                            {
                                Assert.Equal(oneKbString + i, context.Request.Headers["header" + i]);
                            }
                            serverResult.SetResult(0);
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                        return Task.CompletedTask;
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var request = CreateRequestMessage(HttpMethod.Get, url, content: null);
            // The default frame size limit is 16kb, and the total header size limit is 32kb.
            for (var i = 0; i < 20; i++)
            {
                request.Headers.Add("header" + i, oneKbString + i);
            }
            var requestTask = client.SendAsync(request);
            var response = await requestTask.DefaultTimeout();
            await serverResult.Task.DefaultTimeout();
            response.EnsureSuccessStatusCode();

            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("received HEADERS frame for stream ID 1 with length 16384 and flags END_STREAM")));
            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("received CONTINUATION frame for stream ID 1 with length 4395 and flags END_HEADERS")));

            await host.StopAsync().DefaultTimeout();
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ResponseHeaders_MultipleFrames_Accepted(string scheme)
        {
            var oneKbString = new string('a', 1024);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        try
                        {
                            // The default frame size limit is 16kb, and the total header size limit is 64kb.
                            for (var i = 0; i < 59; i++)
                            {
                                context.Response.Headers.Append("header" + i, oneKbString + i);
                            }
                            serverResult.SetResult(0);
                        }
                        catch (Exception ex)
                        {
                            serverResult.SetException(ex);
                        }
                        return Task.CompletedTask;
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            var response = await client.GetAsync(url).DefaultTimeout();
            await serverResult.Task.DefaultTimeout();
            response.EnsureSuccessStatusCode();
            for (var i = 0; i < 59; i++)
            {
                Assert.Equal(oneKbString + i, response.Headers.GetValues("header" + i).Single());
            }

            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("sending HEADERS frame for stream ID 1 with length 15636 and flags END_STREAM")));
            Assert.Equal(2, TestSink.Writes.Where(context => context.Message.Contains("sending CONTINUATION frame for stream ID 1 with length 15585 and flags NONE")).Count());
            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("sending CONTINUATION frame for stream ID 1 with length 14546 and flags END_HEADERS")));

            await host.StopAsync().DefaultTimeout();
        }

        // Settings_Concurrent request limit
        // Settings_FrameSizeLimit
        // Settings_HeaderTableSize
        // Settings_WindowSize
        // Settings_MaxHeaderListSize
        // UnicodeRequestHost
        // Url encoding
        // Header encoding

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var client = new HttpClient(handler);
            client.DefaultRequestVersion = HttpVersion.Version20;
            return client;
        }

        private static HttpRequestMessage CreateRequestMessage(HttpMethod method, string url, HttpContent content)
        {
            return new HttpRequestMessage(method, url)
            {
                Version = HttpVersion.Version20,
                Content = content,
            };
        }

        private static void ConfigureKestrel(IWebHostBuilder webHostBuilder, string scheme)
        {
            webHostBuilder.UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    if (scheme == "https")
                    {
                        listenOptions.UseHttps(TestResources.GetTestCertificate());
                    }
                });
            });
        }

        private static async Task ReadStreamHelloWorld(Stream stream)
        {
            var responseBuffer = new byte["Hello World".Length];
            var read = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length).DefaultTimeout();
            Assert.Equal(responseBuffer.Length, read);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(responseBuffer));
        }
    }
}
