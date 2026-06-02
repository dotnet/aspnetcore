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
        public async Task RefreshAuthAsyncSendsFreshlyFetchedTokenNotCachedConnectToken()
        {
            // Regression test: the AccessTokenHttpMessageHandler caches the access token captured when the
            // connection started and overwrites the Authorization header on each request. RefreshAuthAsync must
            // force a fresh token to be fetched so the /refresh request carries the new token (and the cache is
            // updated for subsequent transport requests).
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

            var currentToken = "old-token";
            Task<string> AccessTokenProvider()
            {
                return Task.FromResult(currentToken);
            }

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, loggerFactory: noErrorScope.LoggerFactory, accessTokenProvider: AccessTokenProvider),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        currentToken = "new-token";
                        await connection.RefreshAuthAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("new-token", request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task RefreshAuthAsyncUpdatesCachedTokenSoSubsequentTransportRequestsUseNewToken()
        {
            // Regression test for the cache side-effect of the IsRefresh fix: AccessTokenHttpMessageHandler caches the
            // access token captured when the connection started and applies it to every transport request. RefreshAuthAsync
            // must update that cache so subsequent Long Polling sends/polls carry the refreshed token, not the stale
            // connect-time token. Asserting only that the /refresh request carries the new token (see the test above) would
            // still pass if the cache update were dropped, silently reintroducing the original bug.
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            // The Long Polling transport reuses the handler's cached token (no IsNegotiate/IsRefresh flag) for each poll,
            // so the Authorization header on a poll reflects whatever the cache currently holds.
            var firstPollReceivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstPollTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var refreshedPollAuthTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pollCount = 0;
            testHttpHandler.OnLongPoll(async (request, _) =>
            {
                var poll = Interlocked.Increment(ref pollCount);
                if (poll == 1)
                {
                    // A poll carrying the connect-time (old) token is in flight. Park it until the refresh completes.
                    firstPollReceivedTcs.TrySetResult();
                    await releaseFirstPollTcs.Task;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }

                // The next poll is issued after the refresh updated the cache; capture its token and end the connection.
                refreshedPollAuthTcs.TrySetResult(request.Headers.Authorization?.Parameter);
                return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
            });

            OnRefresh(testHttpHandler, (_, _) =>
                Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK, "{\"connectionId\":\"abc\",\"tokenLifetimeSeconds\":60}")));

            var currentToken = "old-token";
            Task<string> AccessTokenProvider()
            {
                return Task.FromResult(currentToken);
            }

            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, loggerFactory: noErrorScope.LoggerFactory, accessTokenProvider: AccessTokenProvider),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        await firstPollReceivedTcs.Task.DefaultTimeout();

                        currentToken = "new-token";
                        await connection.RefreshAuthAsync().DefaultTimeout();

                        // Allow the next poll to be issued now that the cache should hold the refreshed token.
                        releaseFirstPollTcs.TrySetResult();

                        var refreshedPollAuth = await refreshedPollAuthTcs.Task.DefaultTimeout();
                        Assert.Equal("new-token", refreshedPollAuth);
                    });
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncDoesNotUpdateCachedTokenWhenServerRejectsRefresh()
        {
            // When the server rejects a refresh (for example an OnAuthRefresh policy denial returning 403), the freshly
            // fetched token must NOT be committed to the handler's cache. Otherwise subsequent Long Polling polls/sends
            // would carry the rejected token and the server would pick it up on the normal poll path, bypassing the
            // refresh policy (and the UserIdentifier-change abort).
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            testHttpHandler.OnNegotiate((_, _) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1)));
            testHttpHandler.OnLongPollDelete(_ => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

            var firstPollReceivedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstPollTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var nextPollAuthTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pollCount = 0;
            testHttpHandler.OnLongPoll(async (request, _) =>
            {
                var poll = Interlocked.Increment(ref pollCount);
                if (poll == 1)
                {
                    // A poll carrying the connect-time (old) token is in flight. Park it until the (rejected) refresh completes.
                    firstPollReceivedTcs.TrySetResult();
                    await releaseFirstPollTcs.Task;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }

                // Capture the token used by the poll issued after the rejected refresh, then end the connection.
                nextPollAuthTcs.TrySetResult(request.Headers.Authorization?.Parameter);
                return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
            });

            OnRefresh(testHttpHandler, (_, _) =>
                Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Forbidden)));

            var currentToken = "old-token";
            Task<string> AccessTokenProvider()
            {
                return Task.FromResult(currentToken);
            }

            await WithConnectionAsync(
                CreateConnection(testHttpHandler, accessTokenProvider: AccessTokenProvider),
                async connection =>
                {
                    await connection.StartAsync().DefaultTimeout();
                    await firstPollReceivedTcs.Task.DefaultTimeout();

                    currentToken = "new-token";
                    await Assert.ThrowsAsync<HttpRequestException>(() => connection.RefreshAuthAsync()).DefaultTimeout();

                    // The rejected refresh must not have poisoned the cache; the next poll keeps the old token.
                    releaseFirstPollTcs.TrySetResult();

                    var nextPollAuth = await nextPollAuthTcs.Task.DefaultTimeout();
                    Assert.Equal("old-token", nextPollAuth);
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
