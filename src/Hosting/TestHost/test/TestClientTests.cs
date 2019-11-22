// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                await ctx.Response.WriteAsync(await new StreamReader(ctx.Request.Body).ReadToEndAsync() + " PUT Response");
            var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var content = new StringContent("Hello world");
            var response = await client.PutAsync("http://localhost:12345", content);

            // Assert
            Assert.Equal("Hello world PUT Response", await response.Content.ReadAsStringAsync());
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
            var response = await client.PostAsync("http://localhost:12345", content);

            // Assert
            Assert.Equal("Hello world POST Response", await response.Content.ReadAsStringAsync());
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
        public async Task WebSocketWorks()
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
    }
}
