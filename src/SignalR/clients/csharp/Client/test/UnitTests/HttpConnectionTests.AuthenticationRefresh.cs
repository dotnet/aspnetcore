// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HttpConnectionTests
{
    public class RefreshAuthentication
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
        public async Task RefreshAuthenticationAsyncSendsFreshlyFetchedTokenNotCachedConnectToken()
        {
            // AccessTokenHttpMessageHandler caches the access token captured when the connection started
            // and overwrites the Authorization header on each request. RefreshAuthenticationAsync must force
            // a fresh token to be fetched so the /refresh request carries the new token.
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
                        await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("new-token", request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncUpdatesCachedTokenSoSubsequentTransportRequestsUseNewToken()
        {
            // RefreshAuthenticationAsync must update the cached access token so subsequent Long Polling
            // sends/polls carry the refreshed token, not the stale connect-time token.
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
                        await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();

                        // Allow the next poll to be issued now that the cache should hold the refreshed token.
                        releaseFirstPollTcs.TrySetResult();

                        var refreshedPollAuth = await refreshedPollAuthTcs.Task.DefaultTimeout();
                        Assert.Equal("new-token", refreshedPollAuth);
                    });
            }
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncDoesNotUpdateCachedTokenWhenServerRejectsRefresh()
        {
            // When the server rejects a refresh (for example an OnAuthenticationRefresh policy denial returning 403), the freshly
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
                    await Assert.ThrowsAsync<HttpRequestException>(() => ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync()).DefaultTimeout();

                    // The rejected refresh must not have poisoned the cache; the next poll keeps the old token.
                    releaseFirstPollTcs.TrySetResult();

                    var nextPollAuth = await nextPollAuthTcs.Task.DefaultTimeout();
                    Assert.Equal("old-token", nextPollAuth);
                });
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncThrowsBeforeConnectionStarted()
        {
            using (var noErrorScope = new VerifyNoErrorsScope())
            {
                var connection = CreateConnection(loggerFactory: noErrorScope.LoggerFactory);
                try
                {
                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout());
                    Assert.Equal("Cannot refresh authentication before the connection is started.", ex.Message);
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
        public async Task RefreshAuthenticationAsyncPostsToRefreshUrlWithConnectionToken(string requestedUrl, string expectedRefreshUrl)
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
                        var ttl = await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                        Assert.Equal(TimeSpan.FromSeconds(60), ttl);
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(expectedRefreshUrl, request.RequestUri.ToString());
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncPostsToOriginalEndpointAfterRedirect()
        {
            // When negotiate redirects the client (e.g. Azure SignalR Service), the transport connects to the
            // redirected endpoint, but /refresh is part of the auth plane and must target the ORIGINAL server
            // endpoint that owns authentication, not the redirected URL.
            var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);
            var negotiateCount = 0;
            testHttpHandler.OnNegotiate((request, _) =>
            {
                negotiateCount++;
                if (negotiateCount == 1)
                {
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        "{\"url\":\"http://redirected.example/hub?existing=true\",\"accessToken\":\"redirect-token\"}");
                }

                return ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent(negotiateVersion: 1));
            });
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
                    CreateConnection(testHttpHandler, url: "http://original.example/hub", loggerFactory: noErrorScope.LoggerFactory),
                    async connection =>
                    {
                        await connection.StartAsync().DefaultTimeout();
                        // The transport followed the redirect to the redirected endpoint.
                        Assert.Equal("http://redirected.example/hub", connection.RemoteEndPoint.ToString());

                        var ttl = await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                        Assert.Equal(TimeSpan.FromSeconds(60), ttl);
                    });
            }
            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("http://original.example/hub/refresh?id=connection-token", request.RequestUri.ToString());
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncSendsBearerTokenWhenAccessTokenProviderConfigured()
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
                        await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("new-token", request.Headers.Authorization.Parameter);
            Assert.True(tokenCallCount >= 1);
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncOmitsAuthorizationHeaderWhenAccessTokenProviderReturnsNull()
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
                        await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                    });
            }

            var request = await refreshRequestTcs.Task.DefaultTimeout();
            Assert.Null(request.Headers.Authorization);
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncReturnsNullWhenTokenLifetimeMissing()
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
                        var ttl = await ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout();
                        Assert.Null(ttl);
                    });
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task RefreshAuthenticationAsyncThrowsOnHttpErrorStatus(HttpStatusCode status)
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
                        () => ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout());
                });
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncPropagatesNetworkException()
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
                        () => ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync().DefaultTimeout());
                    Assert.Equal("network down", ex.Message);
                });
        }

        [Fact]
        public async Task RefreshAuthenticationAsyncPropagatesCancellation()
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
                    var refreshTask = ((IAuthenticationRefreshFeature)connection).RefreshAuthenticationAsync(cts.Token);
                    await requestReceivedTcs.Task.DefaultTimeout();
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refreshTask.DefaultTimeout());
                });
        }
    }
}
