// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public partial class HubConnectionTests
{
    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task CanRefreshAuthAndContinueInvoking(HttpTransportType transportType)
    {
        await using (var server = await StartServer<Startup>())
        {
            async Task<string> AccessTokenProvider()
            {
                var httpResponse = await new HttpClient().GetAsync(server.Url + "/generateJwtToken");
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }

            var hubConnection = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/authRefreshHub", transportType, options =>
                {
                    options.AccessTokenProvider = AccessTokenProvider;
                })
                .WithAuthRefresh(o => o.EnableAutoRefresh = false)
                .Build();
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var before = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.Echo), "hello").DefaultTimeout();
                Assert.Equal("hello", before);

                var newTtl = await hubConnection.RefreshAuthAsync().DefaultTimeout();
                Assert.NotNull(newTtl);
                Assert.True(newTtl > 0, $"Expected a positive token lifetime but got {newTtl}.");

                var after = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.Echo), "world").DefaultTimeout();
                Assert.Equal("world", after);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task RefreshingAuthRemovingClaimBlocksAuthorizedMethod(HttpTransportType transportType)
    {
        await using (var server = await StartServer<Startup>())
        {
            var includeScope = true;
            async Task<string> AccessTokenProvider()
            {
                var url = server.Url + "/generateJwtTokenWithScope?scope=" + (includeScope ? "true" : "false");
                var httpResponse = await new HttpClient().GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }

            var hubConnection = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/authRefreshHub", transportType, options =>
                {
                    options.AccessTokenProvider = AccessTokenProvider;
                })
                .WithAuthRefresh(o => o.EnableAutoRefresh = false)
                .Build();
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // Initial token has the scope claim, so the authorized method succeeds.
                var result = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.Equal("ok", result);

                // Refresh with a token that no longer carries the required claim.
                includeScope = false;
                await hubConnection.RefreshAuthAsync().DefaultTimeout();

                // The authorized method is now rejected by the server's policy.
                await Assert.ThrowsAsync<HubException>(
                    () => hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.ScopeProtected)).DefaultTimeout());

                // An unauthorized method still works, proving the connection itself is alive.
                var echo = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.Echo), "still here").DefaultTimeout();
                Assert.Equal("still here", echo);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task RefreshingAuthAddingClaimAllowsAuthorizedMethod(HttpTransportType transportType)
    {
        await using (var server = await StartServer<Startup>())
        {
            var includeScope = false;
            async Task<string> AccessTokenProvider()
            {
                var url = server.Url + "/generateJwtTokenWithScope?scope=" + (includeScope ? "true" : "false");
                var httpResponse = await new HttpClient().GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }

            var hubConnection = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/authRefreshHub", transportType, options =>
                {
                    options.AccessTokenProvider = AccessTokenProvider;
                })
                .WithAuthRefresh(o => o.EnableAutoRefresh = false)
                .Build();
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                // Initial token lacks the scope claim, so the authorized method is rejected.
                await Assert.ThrowsAsync<HubException>(
                    () => hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.ScopeProtected)).DefaultTimeout());

                // Refresh with a token that now carries the required claim.
                includeScope = true;
                await hubConnection.RefreshAuthAsync().DefaultTimeout();

                // The authorized method now succeeds.
                var result = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.ScopeProtected)).DefaultTimeout();
                Assert.Equal("ok", result);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task CanRefreshAuthAfterReconnect(HttpTransportType transportType)
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   (writeContext.EventId.Name == "ReconnectingWithError" ||
                    // A stale one-shot auth-refresh timer may fire during the reconnect gap and
                    // observe a non-active connection; this is logged and benign.
                    writeContext.EventId.Name == "AuthRefreshFailed");
        }

        await using (var server = await StartServer<Startup>(ExpectedErrors))
        {
            async Task<string> AccessTokenProvider()
            {
                var httpResponse = await new HttpClient().GetAsync(server.Url + "/generateJwtToken");
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }

            var hubConnection = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/authRefreshHub", transportType, options =>
                {
                    options.AccessTokenProvider = AccessTokenProvider;
                })
                .WithAuthRefresh(o => o.EnableAutoRefresh = true)
                .WithAutomaticReconnect()
                .Build();
            try
            {
                var reconnectingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var reconnectedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

                hubConnection.Reconnecting += _ =>
                {
                    reconnectingTcs.TrySetResult();
                    return Task.CompletedTask;
                };
                hubConnection.Reconnected += connectionId =>
                {
                    reconnectedTcs.TrySetResult(connectionId);
                    return Task.CompletedTask;
                };

                await hubConnection.StartAsync().DefaultTimeout();
                var initialConnectionId = hubConnection.ConnectionId;

                // Refresh works on the original connection.
                Assert.NotNull(await hubConnection.RefreshAuthAsync().DefaultTimeout());

                // Force a reconnect.
                hubConnection.OnServerTimeout();

                await reconnectingTcs.Task.DefaultTimeout();
                var newConnectionId = await reconnectedTcs.Task.DefaultTimeout();
                Assert.NotEqual(initialConnectionId, newConnectionId);

                // Refresh must work against the freshly-established connection, proving the
                // IAuthRefreshFeature was re-acquired (and the auto-refresh timer re-armed) on reconnect.
                var newTtl = await hubConnection.RefreshAuthAsync().DefaultTimeout();
                Assert.NotNull(newTtl);
                Assert.True(newTtl > 0, $"Expected a positive token lifetime but got {newTtl}.");

                var echo = await hubConnection.InvokeAsync<string>(nameof(AuthRefreshHub.Echo), "reconnected").DefaultTimeout();
                Assert.Equal("reconnected", echo);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task RefreshChangingUserIdentifierRekeysConnection(HttpTransportType transportType)
    {
        await using (var server = await StartServer<Startup>())
        {
            // The SignalR UserIdentifier is derived from the JWT NameIdentifier claim, so changing
            // the user name on refresh changes the connection's UserIdentifier and should re-key it.
            var userName = "userA";
            async Task<string> AccessTokenProvider()
            {
                var httpResponse = await new HttpClient().GetAsync(server.Url + "/generateJwtToken/" + userName);
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }

            var hubConnection = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/authRefreshHub", transportType, options =>
                {
                    options.AccessTokenProvider = AccessTokenProvider;
                })
                .WithAuthRefresh(o => o.EnableAutoRefresh = false)
                .Build();
            try
            {
                var received = Channel.CreateUnbounded<string>();
                hubConnection.On<string>("Receive", message => received.Writer.TryWrite(message));

                await hubConnection.StartAsync().DefaultTimeout();

                // Before refresh, the connection is reachable under the original user id.
                await hubConnection.InvokeAsync(nameof(AuthRefreshHub.SendToUser), "userA", "before").DefaultTimeout();
                Assert.Equal("before", await received.Reader.ReadAsync().AsTask().DefaultTimeout());

                // Refresh with a token carrying a different NameIdentifier, changing the UserIdentifier.
                userName = "userB";
                await hubConnection.RefreshAuthAsync().DefaultTimeout();

                // The connection is now reachable under the new user id (it was re-keyed, not aborted).
                await hubConnection.InvokeAsync(nameof(AuthRefreshHub.SendToUser), "userB", "after").DefaultTimeout();
                Assert.Equal("after", await received.Reader.ReadAsync().AsTask().DefaultTimeout());

                // The old user id no longer routes to this connection. Send to the stale id (a no-op)
                // followed by the new id; the next message received must be the new-id "sentinel",
                // proving the stale-id message was never delivered.
                await hubConnection.InvokeAsync(nameof(AuthRefreshHub.SendToUser), "userA", "stale").DefaultTimeout();
                await hubConnection.InvokeAsync(nameof(AuthRefreshHub.SendToUser), "userB", "sentinel").DefaultTimeout();
                Assert.Equal("sentinel", await received.Reader.ReadAsync().AsTask().DefaultTimeout());
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
            }
        }
    }
}
