// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class TestClientTests
    {
        [Fact]
        public async Task GetAsyncWorks()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
                ctx.Response.WriteAsync(expected);
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task NoTrailingSlash_NoPathBase()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
            {
                Assert.Equal("", ctx.Request.PathBase.Value);
                Assert.Equal("/", ctx.Request.Path.Value);
                return ctx.Response.WriteAsync(expected);
            };
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task SingleTrailingSlash_NoPathBase()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
            {
                Assert.Equal("", ctx.Request.PathBase.Value);
                Assert.Equal("/", ctx.Request.Path.Value);
                return ctx.Response.WriteAsync(expected);
            };
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345/");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task PutAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
            {
                var content = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
                await ctx.Response.WriteAsync(content + " PUT Response");
            };
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var content = new StringContent("Hello world");
            var response = await client.PutAsync("http://localhost:12345", content).WithTimeout();

            // Assert
            Assert.Equal("Hello world PUT Response", await response.Content.ReadAsStringAsync().WithTimeout());
        }

        [Fact]
        public async Task PostAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                await ctx.Response.WriteAsync(await new StreamReader(ctx.Request.Body).ReadToEndAsync() + " POST Response");
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var content = new StringContent("Hello world");
            var response = await client.PostAsync("http://localhost:12345", content).WithTimeout();

            // Assert
            Assert.Equal("Hello world POST Response", await response.Content.ReadAsStringAsync().WithTimeout());
        }

        [Fact]
        public async Task LargePayload_DisposesRequest_AfterResponseIsCompleted()
        {
            // Arrange
            var data = new byte[2048];
            var character = Encoding.ASCII.GetBytes("a");

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = character[0];
            }

            var builder = new WebHostBuilder();
            RequestDelegate app = async ctx =>
            {
                var disposable = new TestDisposable();
                ctx.Response.RegisterForDispose(disposable);
                await ctx.Response.Body.WriteAsync(data, 0, 1024);

                Assert.False(disposable.IsDisposed);

                await ctx.Response.Body.WriteAsync(data, 1024, 1024);
            };

            builder.Configure(appBuilder => appBuilder.Run(app));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act & Assert
            var response = await client.GetAsync("http://localhost:12345");
        }

        private class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Fact]
        public async Task ClientStreamingWorks()
        {
            // Arrange
            var responseStartedSyncPoint = new SyncPoint();
            var requestEndingSyncPoint = new SyncPoint();
            var requestStreamSyncPoint = new SyncPoint();

            RequestDelegate appDelegate = async ctx =>
            {
                // Send headers
                await ctx.Response.BodyWriter.FlushAsync();

                // Ensure headers received by client
                await responseStartedSyncPoint.WaitToContinue();

                await ctx.Response.WriteAsync("STARTED");

                // ReadToEndAsync will wait until request body is complete
                var requestString = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
                await ctx.Response.WriteAsync(requestString + " POST Response");

                await requestEndingSyncPoint.WaitToContinue();
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamSyncPoint.WaitToContinue();
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            await responseStartedSyncPoint.WaitForSyncPoint().WithTimeout();
            responseStartedSyncPoint.Continue();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert

            // Ensure request stream has started
            await requestStreamSyncPoint.WaitForSyncPoint();

            byte[] buffer = new byte[1024];
            var length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal("STARTED", Encoding.UTF8.GetString(buffer, 0, length));

            // Send content and finish request body
            await requestStream.WriteAsync(Encoding.UTF8.GetBytes("Hello world")).AsTask().WithTimeout();
            await requestStream.FlushAsync().WithTimeout();
            requestStreamSyncPoint.Continue();

            // Ensure content is received while request is in progress
            length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal("Hello world POST Response", Encoding.UTF8.GetString(buffer, 0, length));

            // Request is ending
            await requestEndingSyncPoint.WaitForSyncPoint().WithTimeout();
            requestEndingSyncPoint.Continue();

            // No more response content
            length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal(0, length);
        }

        [Fact]
        public async Task ClientStreaming_Cancellation()
        {
            // Arrange
            var responseStartedSyncPoint = new SyncPoint();
            var responseReadSyncPoint = new SyncPoint();
            var responseEndingSyncPoint = new SyncPoint();
            var requestStreamSyncPoint = new SyncPoint();
            var readCanceled = false;

            RequestDelegate appDelegate = async ctx =>
            {
                // Send headers
                await ctx.Response.BodyWriter.FlushAsync();

                // Ensure headers received by client
                await responseStartedSyncPoint.WaitToContinue();

                var serverBuffer = new byte[1024];
                var serverLength = await ctx.Request.Body.ReadAsync(serverBuffer);

                Assert.Equal("SENT", Encoding.UTF8.GetString(serverBuffer, 0, serverLength));

                await responseReadSyncPoint.WaitToContinue();

                try
                {
                    await ctx.Request.Body.ReadAsync(serverBuffer);
                }
                catch (OperationCanceledException)
                {
                    readCanceled = true;
                }

                await responseEndingSyncPoint.WaitToContinue();
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamSyncPoint.WaitToContinue();
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            await responseStartedSyncPoint.WaitForSyncPoint().WithTimeout();
            responseStartedSyncPoint.Continue();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert

            // Ensure request stream has started
            await requestStreamSyncPoint.WaitForSyncPoint();

            // Write to request
            await requestStream.WriteAsync(Encoding.UTF8.GetBytes("SENT")).AsTask().WithTimeout();
            await requestStream.FlushAsync().WithTimeout();
            await responseReadSyncPoint.WaitForSyncPoint().WithTimeout();

            // Cancel request. Disposing response must be used because SendAsync has finished.
            response.Dispose();
            responseReadSyncPoint.Continue();

            await responseEndingSyncPoint.WaitForSyncPoint().WithTimeout();
            responseEndingSyncPoint.Continue();

            Assert.True(readCanceled);

            requestStreamSyncPoint.Continue();
        }

        [Fact]
        public async Task ClientStreaming_ResponseCompletesWithoutReadingRequest()
        {
            // Arrange
            var requestStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var responseEndingSyncPoint = new SyncPoint();

            RequestDelegate appDelegate = async ctx =>
            {
                await ctx.Response.WriteAsync("POST Response");
                await responseEndingSyncPoint.WaitToContinue();
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamTcs.Task;
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert

            // Read response
            byte[] buffer = new byte[1024];
            var length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal("POST Response", Encoding.UTF8.GetString(buffer, 0, length));

            // Send large content and block on back pressure
            var writeTask = Task.Run(async () =>
            {
                try
                {
                    await requestStream.WriteAsync(Encoding.UTF8.GetBytes(new string('!', 1024 * 1024 * 50))).AsTask().WithTimeout();
                    requestStreamTcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    requestStreamTcs.SetException(ex);
                }
            });

            responseEndingSyncPoint.Continue();

            // No more response content
            length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal(0, length);

            await writeTask;
        }

        [Fact]
        public async Task ClientStreaming_ResponseCompletesWithPendingRead_ThrowError()
        {
            // Arrange
            var requestStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            RequestDelegate appDelegate = async ctx =>
            {
                var pendingReadTask = ctx.Request.Body.ReadAsync(new byte[1024], 0, 1024);
                ctx.Response.Headers["test-header"] = "true";
                await ctx.Response.Body.FlushAsync();
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamTcs.Task;
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("true", response.Headers.GetValues("test-header").Single());

            // Read response
            var ex = await Assert.ThrowsAsync<IOException>(async () =>
            {
                byte[] buffer = new byte[1024];
                var length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            });
            Assert.Equal("An error occurred when completing the request. Request delegate may have finished while there is a pending read of the request body.", ex.InnerException.Message);

            // Unblock request
            requestStreamTcs.TrySetResult(null);
        }

        [Fact]
        public async Task ClientStreaming_ResponseCompletesWithoutResponseBodyWrite()
        {
            // Arrange
            var requestStreamTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            RequestDelegate appDelegate = ctx =>
            {
                ctx.Response.Headers["test-header"] = "true";
                return Task.CompletedTask;
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamTcs.Task;
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("true", response.Headers.GetValues("test-header").Single());

            // Read response
            byte[] buffer = new byte[1024];
            var length = await responseContent.ReadAsync(buffer).AsTask().WithTimeout();
            Assert.Equal(0, length);

            // Writing to request stream will fail because server is complete
            await Assert.ThrowsAnyAsync<Exception>(() => requestStream.WriteAsync(buffer).AsTask());

            // Unblock request
            requestStreamTcs.TrySetResult(null);
        }

        [Fact]
        public async Task ClientStreaming_ServerAbort()
        {
            // Arrange
            var requestStreamSyncPoint = new SyncPoint();
            var responseEndingSyncPoint = new SyncPoint();

            RequestDelegate appDelegate = async ctx =>
            {
                // Send headers
                await ctx.Response.BodyWriter.FlushAsync();

                ctx.Abort();
                await responseEndingSyncPoint.WaitToContinue();
            };

            Stream requestStream = null;

            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345");
            httpRequest.Version = new Version(2, 0);
            httpRequest.Content = new PushContent(async stream =>
            {
                requestStream = stream;
                await requestStreamSyncPoint.WaitToContinue();
            });

            // Act
            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).WithTimeout();

            var responseContent = await response.Content.ReadAsStreamAsync().WithTimeout();

            // Assert

            // Ensure server has aborted
            await responseEndingSyncPoint.WaitForSyncPoint();

            // Ensure request stream has started
            await requestStreamSyncPoint.WaitForSyncPoint();

            // Send content and finish request body
            await ExceptionAssert.ThrowsAsync<OperationCanceledException>(
                () => requestStream.WriteAsync(Encoding.UTF8.GetBytes("Hello world")).AsTask(),
                "Flush was canceled on underlying PipeWriter.").WithTimeout();

            responseEndingSyncPoint.Continue();
            requestStreamSyncPoint.Continue();
        }

        private class PushContent : HttpContent
        {
            private readonly Func<Stream, Task> _sendContent;

            public PushContent(Func<Stream, Task> sendContent)
            {
                _sendContent = sendContent;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return _sendContent(stream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }

        [Fact]
        public async Task WebSocketWorks()
        {
            // Arrange
            // This logger will attempt to access information from HttpRequest once the HttpContext is created
            var logger = new VerifierLogger();
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    Assert.False(ctx.Request.Headers.ContainsKey(HeaderNames.SecWebSocketProtocol));
                    var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    var receiveArray = new byte[1024];
                    while (true)
                    {
                        var receiveResult = await websocket.ReceiveAsync(new System.ArraySegment<byte>(receiveArray), CancellationToken.None);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
                            break;
                        }
                        else
                        {
                            var sendBuffer = new System.ArraySegment<byte>(receiveArray, 0, receiveResult.Count);
                            await websocket.SendAsync(sendBuffer, receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
            };
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILogger<IWebHost>>(logger);
                })
                .Configure(app =>
                {
                    app.Run(appDelegate);
                });
            var server = new TestServer(builder);

            // Act
            var client = server.CreateWebSocketClient();
            // The HttpContext will be created and the logger will make sure that the HttpRequest exists and contains reasonable values
            var clientSocket = await client.ConnectAsync(new System.Uri("http://localhost"), CancellationToken.None);
            var hello = Encoding.UTF8.GetBytes("hello");
            await clientSocket.SendAsync(new System.ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);
            var world = Encoding.UTF8.GetBytes("world!");
            await clientSocket.SendAsync(new System.ArraySegment<byte>(world), WebSocketMessageType.Binary, true, CancellationToken.None);
            await clientSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.CloseSent, clientSocket.State);

            var buffer = new byte[1024];
            var result = await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.Equal(hello.Length, result.Count);
            Assert.True(hello.SequenceEqual(buffer.Take(hello.Length)));
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);

            result = await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.Equal(world.Length, result.Count);
            Assert.True(world.SequenceEqual(buffer.Take(world.Length)));
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

            result = await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketState.Closed, clientSocket.State);

            clientSocket.Dispose();
        }

        [Fact]
        public async Task WebSocketSubProtocolsWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    if (ctx.WebSockets.WebSocketRequestedProtocols.Contains("alpha") &&
                        ctx.WebSockets.WebSocketRequestedProtocols.Contains("bravo"))
                    {
                        // according to rfc6455, the "server needs to include the same field and one of the selected subprotocol values"
                        // however, this isn't enforced by either our server or client so it's possible to accept an arbitrary protocol.
                        // Done here to demonstrate not "correct" behaviour, simply to show it's possible. Other clients may not allow this.
                        var websocket = await ctx.WebSockets.AcceptWebSocketAsync("charlie");
                        await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
                    }
                    else
                    {
                        var subprotocols = ctx.WebSockets.WebSocketRequestedProtocols.Any()
                            ? string.Join(", ", ctx.WebSockets.WebSocketRequestedProtocols)
                            : "<none>";
                        var closeReason = "Unexpected subprotocols: " + subprotocols;
                        var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                        await websocket.CloseAsync(WebSocketCloseStatus.InternalServerError, closeReason, CancellationToken.None);
                    }
                }
            };
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run(appDelegate);
                });
            var server = new TestServer(builder);

            // Act
            var client = server.CreateWebSocketClient();
            client.SubProtocols.Add("alpha");
            client.SubProtocols.Add("bravo");
            var clientSocket = await client.ConnectAsync(new Uri("wss://localhost"), CancellationToken.None);
            var buffer = new byte[1024];
            var result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal("Normal Closure", result.CloseStatusDescription);
            Assert.Equal(WebSocketState.CloseReceived, clientSocket.State);
            Assert.Equal("charlie", clientSocket.SubProtocol);

            clientSocket.Dispose();
        }

        [ConditionalFact]
        public async Task WebSocketAcceptThrowsWhenCancelled()
        {
            // Arrange
            // This logger will attempt to access information from HttpRequest once the HttpContext is created
            var logger = new VerifierLogger();
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    var receiveArray = new byte[1024];
                    while (true)
                    {
                        var receiveResult = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveArray), CancellationToken.None);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
                            break;
                        }
                        else
                        {
                            var sendBuffer = new System.ArraySegment<byte>(receiveArray, 0, receiveResult.Count);
                            await websocket.SendAsync(sendBuffer, receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
            };
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddSingleton<ILogger<IWebHost>>(logger))
                .Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);

            // Act
            var client = server.CreateWebSocketClient();
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.ConnectAsync(new Uri("http://localhost"), tokenSource.Token));
        }

        private class VerifierLogger : ILogger<IWebHost>
        {
            public IDisposable BeginScope<TState>(TState state) => new NoopDispoasble();

            public bool IsEnabled(LogLevel logLevel) => true;

            // This call verifies that fields of HttpRequest are accessed and valid
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => formatter(state, exception);

            class NoopDispoasble : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        [Fact]
        public async Task WebSocketDisposalThrowsOnPeer()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    websocket.Dispose();
                }
            };
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(appDelegate);
            });
            var server = new TestServer(builder);

            // Act
            var client = server.CreateWebSocketClient();
            var clientSocket = await client.ConnectAsync(new System.Uri("http://localhost"), CancellationToken.None);
            var buffer = new byte[1024];
            await Assert.ThrowsAsync<IOException>(async () => await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None));

            clientSocket.Dispose();
        }

        [Fact]
        public async Task WebSocketTinyReceiveGeneratesEndOfMessage()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    var receiveArray = new byte[1024];
                    while (true)
                    {
                        var receiveResult = await websocket.ReceiveAsync(new System.ArraySegment<byte>(receiveArray), CancellationToken.None);
                        var sendBuffer = new System.ArraySegment<byte>(receiveArray, 0, receiveResult.Count);
                        await websocket.SendAsync(sendBuffer, receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);
                    }
                }
            };
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(appDelegate);
            });
            var server = new TestServer(builder);

            // Act
            var client = server.CreateWebSocketClient();
            var clientSocket = await client.ConnectAsync(new System.Uri("http://localhost"), CancellationToken.None);
            var hello = Encoding.UTF8.GetBytes("hello");
            await clientSocket.SendAsync(new System.ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);

            // Assert
            var buffer = new byte[1];
            for (var i = 0; i < hello.Length; i++)
            {
                bool last = i == (hello.Length - 1);
                var result = await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
                Assert.Equal(buffer.Length, result.Count);
                Assert.Equal(buffer[0], hello[i]);
                Assert.Equal(last, result.EndOfMessage);
            }

            clientSocket.Dispose();
        }

        [Fact]
        public async Task ClientDisposalAbortsRequest()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            RequestDelegate appDelegate = async ctx =>
            {
                // Write Headers
                await ctx.Response.Body.FlushAsync();

                var sem = new SemaphoreSlim(0);
                try
                {
                    await sem.WaitAsync(ctx.RequestAborted);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            };

            // Act
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:12345");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            // Abort Request
            response.Dispose();

            // Assert
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await tcs.Task);
        }

        [Fact]
        public async Task ClientCancellationAbortsRequest()
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app => app.Run(async ctx =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ctx.RequestAborted);
                    tcs.SetResult(0);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    return;
                }
                throw new InvalidOperationException("The request was not aborted");
            }));
            using var server = new TestServer(builder);
            using var client = server.CreateClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var response = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("http://localhost:12345", cts.Token));

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await tcs.Task);
        }

        [Fact]
        public async Task AsyncLocalValueOnClientIsNotPreserved()
        {
            var asyncLocal = new AsyncLocal<object>();
            var value = new object();
            asyncLocal.Value = value;

            object capturedValue = null;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run((context) =>
                    {
                        capturedValue = asyncLocal.Value;
                        return context.Response.WriteAsync("Done");
                    });
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var resp = await client.GetAsync("/");

            Assert.NotSame(value, capturedValue);
        }

        [Fact]
        public async Task AsyncLocalValueOnClientIsPreservedIfPreserveExecutionContextIsTrue()
        {
            var asyncLocal = new AsyncLocal<object>();
            var value = new object();
            asyncLocal.Value = value;

            object capturedValue = null;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run((context) =>
                    {
                        capturedValue = asyncLocal.Value;
                        return context.Response.WriteAsync("Done");
                    });
                });
            var server = new TestServer(builder)
            {
                PreserveExecutionContext = true
            };
            var client = server.CreateClient();

            var resp = await client.GetAsync("/");

            Assert.Same(value, capturedValue);
        }

        [Fact]
        public async Task SendAsync_Default_Protocol11()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
                ctx.Response.WriteAsync(expected);
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:12345");

            // Act
            var message = await client.SendAsync(request);
            var actual = await message.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, actual);
            Assert.Equal(new Version(1, 1), message.Version);
        }

        [Fact]
        public async Task SendAsync_ExplicitlySet_Protocol20()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
                ctx.Response.WriteAsync(expected);
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:12345");
            request.Version = new Version(2, 0);

            // Act
            var message = await client.SendAsync(request);
            var actual = await message.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, actual);
            Assert.Equal(new Version(2, 0), message.Version);
        }
    }
}
