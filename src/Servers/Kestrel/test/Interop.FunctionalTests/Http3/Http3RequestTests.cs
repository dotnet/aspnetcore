// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Interop.FunctionalTests.Http3
{
    public class Http3RequestTests : LoggedTest
    {
        private class StreamingHttpContext : HttpContent
        {
            private readonly TaskCompletionSource _completeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<Stream> _getStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _getStreamTcs.TrySetResult(stream);

                await _completeTcs.Task;
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }

            public Task<Stream> GetStreamAsync()
            {
                return _getStreamTcs.Task;
            }

            public void CompleteStream()
            {
                _completeTcs.TrySetResult();
            }
        }

        private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

        [ConditionalFact]
        [MsQuicSupported]
        public async Task POST_ServerCompletsWithoutReadingRequestBody_ClientGetsResponse()
        {
            // Arrange
            var builder = CreateHttp3HostBuilder(async context =>
            {
                var body = context.Request.Body;

                var data = new List<byte>();
                var buffer = new byte[1024];
                var readCount = 0;
                while ((readCount = await body.ReadAsync(buffer).DefaultTimeout()) != -1)
                {
                    data.AddRange(buffer.AsMemory(0, readCount).ToArray());
                    if (data.Count == TestData.Length)
                    {
                        break;
                    }
                }

                await context.Response.Body.WriteAsync(buffer.AsMemory(0, TestData.Length));
            });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                await host.StartAsync();

                var requestContent = new StreamingHttpContext();

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://127.0.0.1:{host.GetPort()}/");
                request.Content = requestContent;
                request.Version = HttpVersion.Version30;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                // Act
                var responseTask = client.SendAsync(request);

                var requestStream = await requestContent.GetStreamAsync();

                // Send headers
                await requestStream.FlushAsync();
                // Write content
                await requestStream.WriteAsync(TestData);

                var response = await responseTask;

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version30, response.Version);
                var responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Hello world", responseText);

                await host.StopAsync();
            }
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task GET_MultipleRequestsInSequence_ReusedState()
        {
            // Arrange
            object persistedState = null;
            var requestCount = 0;

            var builder = CreateHttp3HostBuilder(context =>
            {
                requestCount++;
                var persistentStateCollection = context.Features.Get<IPersistentStateFeature>().State;
                if (persistentStateCollection.TryGetValue("Counter", out var value))
                {
                    persistedState = value;
                }
                persistentStateCollection["Counter"] = requestCount;

                return Task.CompletedTask;
            });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                await host.StartAsync();

                // Act
                var request1 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request1.Version = HttpVersion.Version30;
                request1.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response1 = await client.SendAsync(request1);
                response1.EnsureSuccessStatusCode();
                var firstRequestState = persistedState;

                // Delay to ensure the stream has enough time to return to pool
                await Task.Delay(100);

                var request2 = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{host.GetPort()}/");
                request2.Version = HttpVersion.Version30;
                request2.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

                var response2 = await client.SendAsync(request2);
                response2.EnsureSuccessStatusCode();
                var secondRequestState = persistedState;

                // Assert
                // First request has no persisted state
                Assert.Null(firstRequestState);

                // State persisted on first request was available on the second request
                Assert.Equal(1, secondRequestState);

                await host.StopAsync();
            }
        }

        private IHostBuilder CreateHttp3HostBuilder(RequestDelegate requestDelegate)
        {
            return GetHostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel(o =>
                        {
                            o.ConfigureEndpointDefaults(listenOptions =>
                            {
                                listenOptions.Protocols = HttpProtocols.Http3;
                            });
                        })
                        .UseUrls("https://127.0.0.1:0")
                        .Configure(app =>
                        {
                            app.Run(requestDelegate);
                        });
                })
                .ConfigureServices(AddTestLogging);
        }

        public static IHostBuilder GetHostBuilder(long? maxReadBufferSize = null)
        {
            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseQuic(options =>
                        {
                            options.MaxReadBufferSize = maxReadBufferSize;
                            options.Alpn = "h3-29";
                            options.IdleTimeout = TimeSpan.FromSeconds(20);
                        });
                });
        }
    }
}
