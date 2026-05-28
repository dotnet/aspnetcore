// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HttpConnectionTests
{
    public class RefreshAuth
    {
        private static void OnRefresh(TestHttpMessageHandler handler, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> onRefresh)
        {
            handler.OnRequest((request, next, cancellationToken) =>
            {
                if (request.Method == HttpMethod.Post &&
                    request.RequestUri.AbsolutePath.EndsWith("/refresh", StringComparison.Ordinal))
                {
                    return onRefresh(request, cancellationToken);
                }
                return next();
            });
        }

        [Fact]
        public async Task RefreshAuthAsyncThrowsBeforeConnectionStarted()
        {
            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                var connection = CreateConnection(loggerFactory: noErrorScope.LoggerFactory);
                try
                {
                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => connection.RefreshAuthAsync().DefaultTimeout());
                    Assert.Equal("Cannot refresh auth before the connection is started.", ex.Message);
                }
                finally
                {
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Theory]
        [InlineData("http://fakeuri.org/", "http://fakeuri.org/refresh?id=connection-token")]
        [InlineData("http://fakeuri.org", "http://fakeuri.org/refresh?id=connection-token")]
        [InlineData("http://fakeuri.org/endpoint", "http://fakeuri.org/endpoint/refresh?id=connection-token")]
        [InlineData("http://fakeuri.org/endpoint/", "http://fakeuri.org/endpoint/refresh?id=connection-token")]
        public async Task RefreshAuthAsyncPostsToRefreshUrlWithConnectionToken(string requestedUrl, string expectedRefreshUrl)
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            var refreshRequestTcs = new TaskCompletionSource<HttpRequestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            OnRefresh(testHttpHandler, (request, _) =>
            {
                refreshRequestTcs.TrySetResult(request);
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, "{\"connectionId\":\"abc\",\"tokenLifetimeSeconds\":60}"));
            });

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, url: requestedUrl, loggerFactory: noErrorScope.LoggerFactory),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        var ttl = await connection.RefreshAuthAsync().DefaultTimeout();
                        Assert.Equal(60, ttl);
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(expectedRefreshUrl, request.RequestUri.ToString());
        }

        [Fact]
        public async Task RefreshAuthAsyncSendsBearerTokenWhenAccessTokenProviderConfigured()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            var refreshRequestTcs = new TaskCompletionSource<HttpRequestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            OnRefresh(testHttpHandler, (request, _) =>
            {
                refreshRequestTcs.TrySetResult(request);
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, "{\"connectionId\":\"abc\"}"));
            });

            var tokenCallCount = 0;
            Task<string> AccessTokenProvider()
            {
                Interlocked.Increment(ref tokenCallCount);
                return Task.FromResult("new-token");
            }

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, loggerFactory: noErrorScope.LoggerFactory, accessTokenProvider: AccessTokenProvider),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        await connection.RefreshAuthAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("new-token", request.Headers.Authorization.Parameter);
            Assert.True(tokenCallCount >= 1);
        }

        [Fact]
        public async Task RefreshAuthAsyncOmitsAuthorizationHeaderWhenAccessTokenProviderReturnsNull()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            var refreshRequestTcs = new TaskCompletionSource<HttpRequestMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            OnRefresh(testHttpHandler, (request, _) =>
            {
                refreshRequestTcs.TrySetResult(request);
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, "{\"connectionId\":\"abc\"}"));
            });

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, loggerFactory: noErrorScope.LoggerFactory),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        await connection.RefreshAuthAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.Null(request.Headers.Authorization);
        }

        [Fact]
        public async Task RefreshAuthAsyncReturnsNullWhenTokenLifetimeSecondsMissing()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            OnRefresh(testHttpHandler, (_, _) =>
                Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, "{\"connectionId\":\"abc\"}")));

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, loggerFactory: noErrorScope.LoggerFactory),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        var ttl = await connection.RefreshAuthAsync().DefaultTimeout();
                        Assert.Null(ttl);
                    });
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task RefreshAuthAsyncThrowsOnHttpErrorStatus(HttpStatusCode status)
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            OnRefresh(testHttpHandler, (_, _) => Task.FromResult(ResponseUtils.CreateResponse(status, "{\"error\":\"x\"}")));

            await WithConnectionAsync(
                CreateConnection(testHttpHandler),
                async connection =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await Assert.ThrowsAsync<HttpRequestException>(
                        () => connection.RefreshAuthAsync().DefaultTimeout());
                });
        }

        [Fact]
        public async Task RefreshAuthAsyncPropagatesNetworkException()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            OnRefresh(testHttpHandler, (_, _) => throw new HttpRequestException("network down"));

            await WithConnectionAsync(
                CreateConnection(testHttpHandler),
                async connection =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    var ex = await Assert.ThrowsAsync<HttpRequestException>(
                        () => connection.RefreshAuthAsync().DefaultTimeout());
                    Assert.Equal("network down", ex.Message);
                });
        }

        [Fact]
        public async Task RefreshAuthAsyncPropagatesCancellation()
        {
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPoll(_ => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            var requestReceivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            OnRefresh(testHttpHandler, async (_, ct) =>
            {
                requestReceivedTcs.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return ResponseUtils.CreateResponse(HttpStatusCode.OK);
            });

            await WithConnectionAsync(
                CreateConnection(testHttpHandler),
                async connection =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    using var cts = new CancellationTokenSource();
                    var refreshTask = connection.RefreshAuthAsync(cts.Token);
                    await requestReceivedTcs.Task.DefaultTimeout();
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refreshTask.DefaultTimeout());
                });
        }
    }
}
