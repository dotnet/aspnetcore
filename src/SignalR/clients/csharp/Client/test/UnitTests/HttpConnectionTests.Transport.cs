// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HttpConnectionTests
{
    public class Transport : VerifiableLoggedTest
    {
        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        public async Task HttpConnectionSetsAccessTokenOnAllRequests(HttpTransportType transportType)
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var requestsExecuted = false;
            var callCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            testHttpHandler.OnRequest(async (request, next, token) =>
            {
                Assert.Equal("Bearer", request.Headers.Authorization.Scheme);

                // Call count increments with each call and is used as the access token
                Assert.Equal(callCount.ToString(CultureInfo.InvariantCulture), request.Headers.Authorization.Parameter);

                requestsExecuted = true;

                return await next();
            });

            testHttpHandler.OnRequest((request, next, token) =>
            {
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            });

            Task<string> AccessTokenProvider()
            {
                callCount++;
                return Task.FromResult(callCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: transportType, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello world 1"));
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello world 2"));
                });
            // Fail safe in case the code is modified and some requests don't execute as a result
            Assert.True(requestsExecuted);
            Assert.Equal(1, callCount);
        }

        [Theory]
        [InlineData(HttpTransportType.LongPolling, true)]
        [InlineData(HttpTransportType.ServerSentEvents, false)]
        public async Task HttpConnectionSetsInherentKeepAliveFeature(HttpTransportType transportType, bool expectedValue)
        {
            using (StartVerifiableLog())
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                testHttpHandler.OnNegotiate((_, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent()));

                testHttpHandler.OnRequest((request, next, token) => Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent)));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, transportType: transportType, loggerFactory: LoggerFactory),
                    async (connection) =>
                    {
                        await connection.StartAsync().DefaultTimeout();

                        var feature = connection.Features.Get<IConnectionInherentKeepAliveFeature>();
                        Assert.NotNull(feature);
                        Assert.Equal(expectedValue, feature.HasInherentKeepAlive);
                    });
            }
        }

        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        public async Task HttpConnectionSetsUserAgentOnAllRequests(HttpTransportType transportType)
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var requestsExecuted = false;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            testHttpHandler.OnRequest(async (request, next, token) =>
            {
                var userAgentHeader = request.Headers.UserAgent.ToString();

                Assert.NotNull(userAgentHeader);
                Assert.StartsWith("Microsoft SignalR/", userAgentHeader);

                // user agent version should come from version embedded in assembly metadata
                var assemblyVersion = typeof(Constants)
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                Assert.Contains(assemblyVersion.InformationalVersion, userAgentHeader);

                requestsExecuted = true;

                return await next();
            });

            testHttpHandler.OnRequest((request, next, token) =>
            {
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            });

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: transportType),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));
                });
            // Fail safe in case the code is modified and some requests don't execute as a result
            Assert.True(requestsExecuted);
        }

        [Theory]
        [InlineData(HttpTransportType.LongPolling)]
        [InlineData(HttpTransportType.ServerSentEvents)]
        public async Task HttpConnectionSetsRequestedWithOnAllRequests(HttpTransportType transportType)
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var requestsExecuted = false;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            testHttpHandler.OnRequest(async (request, next, token) =>
            {
                var requestedWithHeader = request.Headers.GetValues(HeaderNames.XRequestedWith);
                var requestedWithValue = Assert.Single(requestedWithHeader);
                Assert.Equal("XMLHttpRequest", requestedWithValue);

                requestsExecuted = true;

                return await next();
            });

            testHttpHandler.OnRequest((request, next, token) =>
            {
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            });

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: transportType),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));
                });
            // Fail safe in case the code is modified and some requests don't execute as a result
            Assert.True(requestsExecuted);
        }

        [Fact]
        public async Task CanReceiveData()
        {
            var testHttpHandler = new TestHttpMessageHandler();

            // Set the long poll up to return a single message over a few polls.
            var requestCount = 0;
            var messageFragments = new[] { "This ", "is ", "a ", "test" };
            testHttpHandler.OnLongPoll(cancellationToken =>
            {
                if (requestCount >= messageFragments.Length)
                {
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                }

                var resp = ResponseUtils.CreateResponse(HttpStatusCode.OK, messageFragments[requestCount]);
                requestCount += 1;
                return resp;
            });
            testHttpHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            await WithConnectionAsync(
                CreateConnection(testHttpHandler),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    Assert.Contains("This is a test", Encoding.UTF8.GetString(await connection.Transport.Input.ReadAllAsync()));
                });
        }

        [Fact]
        public async Task CanSendData()
        {
            var data = new byte[] { 1, 1, 2, 3, 5, 8 };

            var testHttpHandler = new TestHttpMessageHandler();

            var sendTcs = new TaskCompletionSource<byte[]>();
            var longPollTcs = new TaskCompletionSource<HttpResponseMessage>();

            testHttpHandler.OnLongPoll(cancellationToken => longPollTcs.Task);

            testHttpHandler.OnSocketSend((buf, cancellationToken) =>
            {
                sendTcs.TrySetResult(buf);
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            });

            await WithConnectionAsync(
                CreateConnection(testHttpHandler),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();

                    await connection.Transport.Output.WriteAsync(data).DefaultTimeout();

                    Assert.Equal(data, await sendTcs.Task.DefaultTimeout());

                    longPollTcs.TrySetResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                });
        }

        [Fact]
        public Task SendThrowsIfConnectionIsNotStarted()
        {
            return WithConnectionAsync(
                CreateConnection(),
                async (connection) =>
                {
                    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => connection.Transport.Output.WriteAsync(new byte[0]).DefaultTimeout());
                    Assert.Equal($"Cannot access the {nameof(Transport)} pipe before the connection has started.", exception.Message);
                });
        }

        [Fact]
        public Task TransportPipeCannotBeAccessedAfterConnectionIsDisposed()
        {
            return WithConnectionAsync(
                CreateConnection(),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();

                    var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
                        () => connection.Transport.Output.WriteAsync(new byte[0]).DefaultTimeout());
                    Assert.Equal(typeof(HttpConnection).FullName, exception.ObjectName);
                });
        }

        [Fact]
        public Task TransportIsShutDownAfterDispose()
        {
            var transport = new TestTransport();
            return WithConnectionAsync(
                CreateConnection(transport: transport),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();

                    // This will throw OperationCanceledException if it's forcibly terminated
                    // which we don't want
                    await transport.Receiving.DefaultTimeout();
                });
        }

        [Fact]
        public Task StartAsyncTransferFormatOverridesOptions()
        {
            var transport = new TestTransport();

            return WithConnectionAsync(
                CreateConnection(transport: transport, transferFormat: TransferFormat.Binary),
                async (connection) =>
                {
                    await connection.StartAsync(TransferFormat.Text).DefaultTimeout();

                    Assert.Equal(TransferFormat.Text, transport.Format);
                });
        }

        [Fact]
        public async Task HttpConnectionFailsOnNegotiateWhenAuthFails()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var accessTokenCallCount = 0;
            var negotiateCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                negotiateCount++;
                return ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized);
            });

            Task<string> AccessTokenProvider()
            {
                accessTokenCallCount++;
                return Task.FromResult(accessTokenCallCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: HttpTransportType.ServerSentEvents, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await Assert.ThrowsAsync<HttpRequestException>(async () => await connection.StartAsync().DefaultTimeout());
                });
            Assert.Equal(1, negotiateCount);
            Assert.Equal(1, accessTokenCallCount);
        }

        [Fact]
        public async Task HttpConnectionRetriesAccessTokenProviderWhenAuthFailsLongPolling()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var requestsExecuted = false;
            var accessTokenCallCount = 0;
            var pollCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            var startSendTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var longPollTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var messageFragments = new[] { "This ", "is ", "a ", "test" };
            testHttpHandler.OnLongPoll(async _ =>
            {
                // fail every other request
                if (pollCount % 2 == 0)
                {
                    pollCount++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized);
                }
                if (pollCount / 2 >= messageFragments.Length)
                {
                    startSendTcs.SetResult();
                    await longPollTcs.Task;
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                }

                var resp = ResponseUtils.CreateResponse(HttpStatusCode.OK, messageFragments[pollCount / 2]);
                pollCount++;
                return resp;
            });

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            testHttpHandler.OnRequest((request, next, token) =>
            {
                if (!requestsExecuted)
                {
                    requestsExecuted = true;
                    return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized));
                }

                Assert.Equal("Bearer", request.Headers.Authorization.Scheme);

                Assert.Equal(accessTokenCallCount.ToString(CultureInfo.InvariantCulture), request.Headers.Authorization.Parameter);

                tcs.SetResult();

                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK));
            });

            Task<string> AccessTokenProvider()
            {
                accessTokenCallCount++;
                return Task.FromResult(accessTokenCallCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: HttpTransportType.LongPolling, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    var message = await connection.Transport.Input.ReadAtLeastAsync(14);
                    Assert.Equal("This is a test", Encoding.UTF8.GetString(message.Buffer));
                    await startSendTcs.Task;
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello world 1"));
                    await tcs.Task;
                    longPollTcs.SetResult();
                });
            // 1 negotiate + 4 (number of polls) + 1 for last poll + 1 for send
            Assert.Equal(7, accessTokenCallCount);
        }

        [Fact]
        public async Task HttpConnectionFailsAfterFirstRetryFailsLongPolling()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var accessTokenCallCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            testHttpHandler.OnLongPoll(_ =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized);
            });

            Task<string> AccessTokenProvider()
            {
                accessTokenCallCount++;
                return Task.FromResult(accessTokenCallCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: HttpTransportType.LongPolling, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await Assert.ThrowsAsync<HttpRequestException>(async () => await connection.Transport.Input.ReadAllAsync());
                });

            // 1 negotiate + 1 retry initial poll
            Assert.Equal(2, accessTokenCallCount);
        }

        [Fact]
        public async Task HttpConnectionRetriesAccessTokenProviderWhenAuthFailsServerSentEvents()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var requestsExecuted = false;
            var accessTokenCallCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            var sendRequestExecuted = false;
            var sendFinishedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            testHttpHandler.OnSocketSend((_, _) =>
            {
                if (!sendRequestExecuted)
                {
                    sendRequestExecuted = true;
                    return ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized);
                }
                sendFinishedTcs.SetResult();
                return ResponseUtils.CreateResponse(HttpStatusCode.OK);
            });

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stream = new BlockingStream(tcs);
            testHttpHandler.OnRequest((request, next, token) =>
            {
                if (!requestsExecuted)
                {
                    requestsExecuted = true;
                    return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized));
                }

                Assert.Equal("Bearer", request.Headers.Authorization.Scheme);

                Assert.Equal(accessTokenCallCount.ToString(CultureInfo.InvariantCulture), request.Headers.Authorization.Parameter);

                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, new StreamContent(stream)));
            });

            Task<string> AccessTokenProvider()
            {
                accessTokenCallCount++;
                return Task.FromResult(accessTokenCallCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: HttpTransportType.ServerSentEvents, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello world 1"));
                    await sendFinishedTcs.Task;
                    tcs.TrySetResult();
                    await connection.Transport.Input.ReadAllAsync();
                });
            // 1 negotiate + 1 retry stream request + 1 retry send
            Assert.Equal(3, accessTokenCallCount);
        }

        [Fact]
        public async Task HttpConnectionFailsAfterFirstRetryFailsServerSentEvents()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var accessTokenCallCount = 0;

            testHttpHandler.OnNegotiate((_, cancellationToken) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent());
            });

            testHttpHandler.OnSocketSend((_, _) =>
            {
                return ResponseUtils.CreateResponse(HttpStatusCode.Unauthorized);
            });

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stream = new BlockingStream(tcs);
            testHttpHandler.OnRequest((request, next, token) =>
            {
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, new StreamContent(stream)));
            });

            Task<string> AccessTokenProvider()
            {
                accessTokenCallCount++;
                return Task.FromResult(accessTokenCallCount.ToString(CultureInfo.InvariantCulture));
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, transportType: HttpTransportType.ServerSentEvents, accessTokenProvider: AccessTokenProvider),
                async (connection) =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello world 1"));
                    await Assert.ThrowsAsync<HttpRequestException>(async () => await connection.Transport.Input.ReadAllAsync());
                });
            // 1 negotiate + 1 retry stream request
            Assert.Equal(2, accessTokenCallCount);
        }

        private class BlockingStream : Stream
        {
            private readonly TaskCompletionSource _tcs;
            private bool _ignoreFirstWrite = true;

            public BlockingStream(TaskCompletionSource tcs)
            {
                _tcs = tcs;
            }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public override void Flush()
            {
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.Register(() => _tcs.TrySetResult());
                await _tcs.Task;
                return 0;
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }
            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (_ignoreFirstWrite)
                {
                    // SSE does an initial write of :\r\n that we want to ignore in testing
                    _ignoreFirstWrite = false;
                    return;
                }
                await _tcs.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_ignoreFirstWrite)
                {
                    // SSE does an initial write of :\r\n that we want to ignore in testing
                    _ignoreFirstWrite = false;
                    return;
                }
                await _tcs.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
