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

        [Theory]
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

        [Theory]
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
        [Theory]
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
        [Theory]
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
            private static readonly int Repetitions = 200;

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
                for (var i = 0; i < Repetitions; i++)
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
                    Assert.True(totalRead <= Repetitions * Content.Length, "Too Long");

                    for (var offset = 0; offset < read; offset++)
                    {
                        Assert.Equal(Content[patternOffset % Content.Length], buffer[offset]);
                        patternOffset++;
                    }

                    using var timer = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    read = await stream.ReadAsync(buffer, 0, buffer.Length, timer.Token).DefaultTimeout();
                }

                Assert.True(totalRead == Repetitions * Content.Length, "Too Short");
            }
        }

        [Theory]
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

        [Theory]
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

        [Theory]
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
            // The client doesn't flush the headers for requests with a body unless a continue is expected.
            client.DefaultRequestHeaders.ExpectContinue = true;

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

            public Task SendStarted => _sendStarted.Task;

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

            internal void Abort()
            {
                if (_sendComplete == null)
                {
                    throw new InvalidOperationException("Sending hasn't started yet.");
                }
                _sendComplete.TrySetException(new Exception("Abort"));
            }
        }

        [Theory]
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

        [Theory]
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

        [Theory]
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

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ServerReset_AfterHeaders_ClientBodyThrows(string scheme)
        {
            var receivedHeaders = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await context.Response.BodyWriter.FlushAsync();
                        await receivedHeaders.Task.DefaultTimeout();
                        context.Features.Get<IHttpResetFeature>().Reset(8); // Cancel
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            receivedHeaders.SetResult(0);
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync()).DefaultTimeout();
            Assert.Equal("The HTTP/2 server reset the stream. HTTP/2 error code 'CANCEL' (0x8).", exception?.InnerException?.InnerException.Message);
            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
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

        [Theory]
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

        [Theory]
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
            // The client doesn't flush the headers for requests with a body unless a continue is expected.
            client.DefaultRequestHeaders.ExpectContinue = true;

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

        [Theory]
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
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
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

        [Theory]
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
            // The client doesn't flush the headers for requests with a body unless a continue is expected.
            client.DefaultRequestHeaders.ExpectContinue = true;

            var streamingContent = new StreamingContent();
            var request = CreateRequestMessage(HttpMethod.Post, url, streamingContent);
            var requestTask = client.SendAsync(request);
            await requestReceived.Task.DefaultTimeout();
            await streamingContent.SendStarted.DefaultTimeout();
            streamingContent.Abort();
            await serverResult.Task.DefaultTimeout();
            await Assert.ThrowsAnyAsync<Exception>(() => requestTask).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
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
                            await ReadStreamHelloWorld(context.Request.Body);
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
            var requestTask = client.SendAsync(request);
            await streamingContent.SendAsync("Hello World").DefaultTimeout();
            await requestReceived.Task.DefaultTimeout();
            streamingContent.Abort();
            await serverResult.Task.DefaultTimeout();
            await Assert.ThrowsAnyAsync<Exception>(() => requestTask).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
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

        [Theory]
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
            await ReadStreamHelloWorld(responseStream);
            responseStream.Dispose(); // Sends reset
            await serverResult.Task.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
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
            await ReadStreamHelloWorld(responseStream);
            responseStream.Dispose(); // Sends reset
            await serverResult.Task.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [QuarantinedTest("https://github.com/dotnet/runtime/issues/860")]
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
            request.Headers.Host = "localhost"; // The default Host header has a random port value which can cause the length to vary.
            var requestTask = client.SendAsync(request);
            var response = await requestTask.DefaultTimeout();
            await serverResult.Task.DefaultTimeout();
            response.EnsureSuccessStatusCode();

            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("received HEADERS frame for stream ID 1 with length 16384 and flags END_STREAM")));
            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("received CONTINUATION frame for stream ID 1 with length 4390 and flags END_HEADERS")));

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
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

            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("sending HEADERS frame for stream ID 1 with length 15612 and flags END_STREAM")));
            Assert.Equal(2, TestSink.Writes.Where(context => context.Message.Contains("sending CONTINUATION frame for stream ID 1 with length 15585 and flags NONE")).Count());
            Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("sending CONTINUATION frame for stream ID 1 with length 14546 and flags END_HEADERS")));

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        // Expect this to change when the client implements dynamic request header compression.
        // Will the client send the first headers before receiving our settings frame?
        // We'll probably need to ensure the settings changes are ack'd before enforcing them.
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_HeaderTableSize_CanBeReduced_Server(string scheme)
        {
            var oneKbString = new string('a', 1024);
            var serverResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureKestrel(options =>
                    {
                        // Must be larger than 0, should disable header compression
                        options.Limits.Http2.HeaderTableSize = 1;
                    });
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        try
                        {
                            for (var i = 0; i < 14; i++)
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
            for (var i = 0; i < 14; i++)
            {
                request.Headers.Add("header" + i, oneKbString + i);
            }
            request.Headers.Host = "localhost"; // The default Host has a random port value that causes the length to very.
            var requestTask = client.SendAsync(request);
            var response = await requestTask.DefaultTimeout();
            await serverResult.Task.DefaultTimeout();
            response.EnsureSuccessStatusCode();

            Assert.Single(TestSink.Writes.Where(context
                => context.Message.Contains("received HEADERS frame for stream ID 1 with length 14540 and flags END_STREAM, END_HEADERS")));

            await host.StopAsync().DefaultTimeout();
        }

        // Settings_HeaderTableSize_CanBeReduced_Client - The client uses the default 4k HPACK dynamic table size and it cannot be changed.
        // Nor does Kestrel yet support sending dynamic table updates, so there's nothing to test here. https://github.com/dotnet/aspnetcore/issues/4715

        [Theory]
        [QuarantinedTest]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_MaxConcurrentStreamsGet_Server(string scheme)
        {
            var sync = new SemaphoreSlim(5);
            var requestCount = 0;
            var requestBlock = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureKestrel(options =>
                    {
                        options.Limits.Http2.MaxStreamsPerConnection = 5;
                    });
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        // The stream limit should mean we never hit the semaphore limit.
                        Assert.True(sync.Wait(0));
                        var count = Interlocked.Increment(ref requestCount);

                        if (count == 5)
                        {
                            requestBlock.TrySetResult(0);
                        }
                        else
                        {
                            await requestBlock.Task.DefaultTimeout();
                        }

                        sync.Release();
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var tasks = new List<Task<HttpResponseMessage>>(10);
            for (var i = 0; i < 10; i++)
            {
                var requestTask = client.GetAsync(url).DefaultTimeout();
                tasks.Add(requestTask);
            }

            var responses = await Task.WhenAll(tasks.ToList()).DefaultTimeout();
            foreach (var response in responses)
            {
                response.EnsureSuccessStatusCode();
            }

            // SKIP: https://github.com/dotnet/aspnetcore/issues/17842
            // The client initially issues all 10 requests before receiving the settings, has 5 refused (after receiving the settings),
            // waits for the first 5 to finish, retries the refused 5, and in the end each request completes successfully despite the logged errors.
            // Assert.Empty(TestSink.Writes.Where(context => context.Message.Contains("HTTP/2 stream error")));

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_MaxConcurrentStreamsPost_Server(string scheme)
        {
            var sync = new SemaphoreSlim(5);
            var requestCount = 0;
            var requestBlock = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureKestrel(options =>
                    {
                        options.Limits.Http2.MaxStreamsPerConnection = 5;
                    });
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        // The stream limit should mean we never hit the semaphore limit.
                        Assert.True(sync.Wait(0));
                        var count = Interlocked.Increment(ref requestCount);

                        if (count == 5)
                        {
                            requestBlock.TrySetResult(0);
                        }
                        else
                        {
                            await requestBlock.Task.DefaultTimeout();
                        }

                        sync.Release();
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();

            var tasks = new List<Task<HttpResponseMessage>>(10);
            for (var i = 0; i < 10; i++)
            {
                var requestTask = client.PostAsync(url, new StringContent("Hello World")).DefaultTimeout();
                tasks.Add(requestTask);
            }

            var responses = await Task.WhenAll(tasks.ToList()).DefaultTimeout();
            foreach (var response in responses)
            {
                response.EnsureSuccessStatusCode();
            }

            // SKIP: https://github.com/dotnet/aspnetcore/issues/17842
            // The client initially issues all 10 requests before receiving the settings, has 5 refused (after receiving the settings),
            // waits for the first 5 to finish, retries the refused 5, and in the end each request completes successfully despite the logged errors.
            // Assert.Empty(TestSink.Writes.Where(context => context.Message.Contains("HTTP/2 stream error")));

            await host.StopAsync().DefaultTimeout();
        }

        // Settings_MaxConcurrentStreams_Client - Neither client or server support Push, nothing to test in this direction.

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_MaxFrameSize_Larger_Server(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureKestrel(options => options.Limits.Http2.MaxFrameSize = 1024 * 20); // The default is 16kb
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context => context.Response.WriteAsync("Hello World")));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            // Send an initial request to ensure the settings get synced before the real test.
            var responseBody = await client.GetStringAsync(url).DefaultTimeout();
            Assert.Equal("Hello World", responseBody);

            var response = await client.PostAsync(url, new ByteArrayContent(new byte[1024 * 18])).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());

            // SKIP: The client does not take advantage of a larger allowed frame size.
            // https://github.com/dotnet/runtime/blob/48a78bfa13e9c710851690621fc2c0fe1637802c/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/Http2Connection.cs#L483-L488
            // Assert.Single(TestSink.Writes.Where(context => context.Message.Contains("received DATA frame for stream ID 1 with length 18432 and flags NONE")));

            await host.StopAsync().DefaultTimeout();
        }

        // Settings_MaxFrameSize_Larger_Client - Not configurable

        [Theory]
        [QuarantinedTest("https://github.com/dotnet/runtime/issues/860")]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_MaxHeaderListSize_Server(string scheme)
        {
            var oneKbString = new string('a', 1024);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context => throw new NotImplementedException()));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            // There's no point in waiting for the settings to sync, the client doesn't check the header list size setting.
            // https://github.com/dotnet/runtime/blob/48a78bfa13e9c710851690621fc2c0fe1637802c/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/Http2Connection.cs#L467-L494

            var request = CreateRequestMessage(HttpMethod.Get, url, content: null);
            // The default size limit is 32kb.
            for (var i = 0; i < 33; i++)
            {
                request.Headers.Add("header" + i, oneKbString + i);
            }
            // Kestrel closes the connection rather than sending the recommended 431 response. https://github.com/dotnet/aspnetcore/issues/17861
            await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request)).DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_MaxHeaderListSize_Client(string scheme)
        {
            var oneKbString = new string('a', 1024);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        // The total header size limit is 64kb.
                        for (var i = 0; i < 65; i++)
                        {
                            context.Response.Headers.Append("header" + i, oneKbString + i);
                        }
                        return Task.CompletedTask;
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);

            using var client = CreateClient();
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)).DefaultTimeout();
            Assert.Equal("The HTTP response headers length exceeded the set limit of 65536 bytes.", ex.InnerException?.InnerException?.Message);

            await host.StopAsync().DefaultTimeout();
        }

        // Settings_InitialWindowSize_Lower_Server - Kestrel does not support lowering the InitialStreamWindowSize below the spec default 64kb.
        // Settings_InitialWindowSize_Lower_Client - Not configurable.

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_InitialWindowSize_Server(string scheme)
        {
            var requestFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await requestFinished.Task.DefaultTimeout();

                        await context.Response.WriteAsync("Hello World");
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();

            var streamingContent = new StreamingContent();
            var requestTask = client.PostAsync(url, streamingContent);

            // The spec default window is 64kb-1 and Kestrel's default is 96kb.
            // We should be able to send the entire request body without getting blocked by flow control.
            var oneKbString = new string('a', 1024);
            for (var i = 0; i < 96; i++)
            {
                await streamingContent.SendAsync(oneKbString).DefaultTimeout();
            }
            streamingContent.Complete();
            requestFinished.SetResult(0);

            var response = await requestTask.DefaultTimeout();
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task Settings_InitialWindowSize_Client(string scheme)
        {
            var responseFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        // The spec default window is 64kb - 1.
                        // We should be able to send the entire response body without getting blocked by flow control.
                        var oneKbString = new string('a', 1024);
                        for (var i = 0; i < 63; i++)
                        {
                            await context.Response.WriteAsync(oneKbString).DefaultTimeout();
                        }
                        await context.Response.WriteAsync(new string('a', 1023)).DefaultTimeout();
                        await context.Response.CompleteAsync().DefaultTimeout();
                        responseFinished.SetResult(0);
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();

            var requestTask = client.GetStreamAsync(url);
            await responseFinished.Task.DefaultTimeout();
            var response = await requestTask.DefaultTimeout();

            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task ConnectionWindowSize_Server(string scheme)
        {
            var requestFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(async context =>
                    {
                        await requestFinished.Task.DefaultTimeout();
                        var buffer = new byte[1024];
                        var read = 0;
                        do
                        {
                            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length).DefaultTimeout();
                        } while (read > 0);

                        await context.Response.WriteAsync("Hello World");
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();

            var streamingContent0 = new StreamingContent();
            var streamingContent1 = new StreamingContent();
            var requestTask0 = client.PostAsync(url, streamingContent0);
            var requestTask1 = client.PostAsync(url, streamingContent1);

            // The spec default connection window is 64kb-1 and Kestrel's default is 128kb.
            // We should be able to send two 64kb requests without getting blocked by flow control.
            var oneKbString = new string('a', 1024);
            for (var i = 0; i < 64; i++)
            {
                await streamingContent0.SendAsync(oneKbString).DefaultTimeout();
                await streamingContent1.SendAsync(oneKbString).DefaultTimeout();
            }
            streamingContent0.Complete();
            streamingContent1.Complete();
            requestFinished.SetResult(0);

            var response0 = await requestTask0.DefaultTimeout();
            var response1 = await requestTask0.DefaultTimeout();
            response0.EnsureSuccessStatusCode();
            response1.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response0.Content.ReadAsStringAsync());
            Assert.Equal("Hello World", await response1.Content.ReadAsStringAsync());

            await host.StopAsync().DefaultTimeout();
        }

        // ConnectionWindowSize_Client - impractical
        // The spec default connection window is 64kb - 1 but the client default is 64Mb (not configurable).
        // The client restricts streams to 64kb - 1 so we would need to issue 64 * 1024 requests to stress the connection window limit.

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task UnicodeRequestHost(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context =>
                    {
                        Assert.Equal("點.看", context.Request.Host.Host);
                        return context.Response.WriteAsync(context.Request.Host.Host);
                    }));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            var request = CreateRequestMessage(HttpMethod.Get, url, content: null);
            request.Headers.Host = "xn--md7a.xn--c1y"; // Punycode
            var response = await client.SendAsync(request).DefaultTimeout();
            response.EnsureSuccessStatusCode();
            Assert.Equal("點.看", await response.Content.ReadAsStringAsync());
            await host.StopAsync().DefaultTimeout();
        }

        [Theory]
        [MemberData(nameof(SupportedSchemes))]
        public async Task UrlEncoding(string scheme)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    ConfigureKestrel(webHostBuilder, scheme);
                    webHostBuilder.ConfigureServices(AddTestLogging)
                    .Configure(app => app.Run(context => context.Response.WriteAsync(context.Request.Path.Value)));
                });
            using var host = await hostBuilder.StartAsync().DefaultTimeout();

            var url = host.MakeUrl(scheme);
            using var client = CreateClient();
            // Skipped controls, '?' and '#'.
            var response = await client.GetAsync(url + " !\"$%&'()*++,-./0123456789:;<>=@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`{|}~點看").DefaultTimeout();
            response.EnsureSuccessStatusCode();
            Assert.Equal("/ !\"$%&'()*++,-./0123456789:;<>=@ABCDEFGHIJKLMNOPQRSTUVWXYZ[/]^_`{|}~點看", await response.Content.ReadAsStringAsync());
            await host.StopAsync().DefaultTimeout();
        }

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
            var totalRead = 0;
            do
            {
                var read = await stream.ReadAsync(responseBuffer, totalRead, responseBuffer.Length - totalRead).DefaultTimeout();
                totalRead += read;
                if (read == 0)
                {
                    throw new InvalidOperationException("Unexpected end of stream");
                }
            } while (totalRead < responseBuffer.Length);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(responseBuffer));
        }
    }
}
