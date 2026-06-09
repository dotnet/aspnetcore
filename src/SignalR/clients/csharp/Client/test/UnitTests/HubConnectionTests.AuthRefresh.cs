// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HubConnectionTests
{
    public class AuthRefresh : VerifiableLoggedTest
    {
        private static HubConnection BuildHubConnection(
            TestConnection connection,
            Action<IHubConnectionBuilder> configure = null)
        {
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");

            var delegateConnectionFactory = new DelegateConnectionFactory(
                async endPoint =>
                {
                    connection.RemoteEndPoint = endPoint;
                    return await connection.StartAsync();
                });

            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            configure?.Invoke(builder);

            return builder.Build();
        }

        private static Timer GetAuthRefreshTimer(HubConnection hubConnection)
        {
            var field = typeof(HubConnection).GetField("_authRefreshTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Timer)field.GetValue(hubConnection);
        }

        private static TimeSpan GetLastAuthRefreshDelay(HubConnection hubConnection)
        {
            var field = typeof(HubConnection).GetField("_lastAuthRefreshDelay", BindingFlags.NonPublic | BindingFlags.Instance);
            return (TimeSpan)field.GetValue(hubConnection);
        }

        [Fact]
        public async Task RefreshAuthAsyncThrowsWhenConnectionNotActive()
        {
            using (StartVerifiableLog())
            {
                var connection = new TestConnection();
                var hubConnection = BuildHubConnection(connection);
                try
                {
                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => hubConnection.RefreshAuthAsync()).DefaultTimeout();
                    Assert.Contains("not active", ex.Message);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncThrowsWhenFeatureMissing()
        {
            using (StartVerifiableLog())
            {
                var connection = new TestConnection();
                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => hubConnection.RefreshAuthAsync()).DefaultTimeout();
                    Assert.Contains("HTTP-based connections", ex.Message);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncInvokesFeatureAndReturnsTtl()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { NextTtlSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);
                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    var ttl = await hubConnection.RefreshAuthAsync().DefaultTimeout();

                    Assert.Equal(3600, ttl);
                    Assert.Equal(1, feature.RefreshCallCount);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncInvokesOnRefreshedCallback()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { NextTtlSeconds = 1800 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                AuthRefreshedContext capturedContext = null;
                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o =>
                    {
                        o.EnableAutoRefresh = false;
                        o.OnRefreshed = ctx =>
                        {
                            capturedContext = ctx;
                            return Task.CompletedTask;
                        };
                    });
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();
                    await hubConnection.RefreshAuthAsync().DefaultTimeout();

                    Assert.NotNull(capturedContext);
                    Assert.Same(hubConnection, capturedContext.HubConnection);
                    Assert.Equal(1800, capturedContext.NewTokenLifetimeSeconds);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncInvokesOnRefreshFailedCallbackAndRethrows()
        {
            using (StartVerifiableLog())
            {
                var thrown = new InvalidOperationException("refresh boom");
                var feature = new FakeAuthRefreshFeature { ExceptionToThrow = thrown };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                AuthRefreshFailedContext capturedContext = null;
                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o =>
                    {
                        o.EnableAutoRefresh = false;
                        o.OnRefreshFailed = ctx =>
                        {
                            capturedContext = ctx;
                            return Task.CompletedTask;
                        };
                    });
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => hubConnection.RefreshAuthAsync()).DefaultTimeout();

                    Assert.Same(thrown, ex);
                    Assert.NotNull(capturedContext);
                    Assert.Same(thrown, capturedContext.Exception);
                    Assert.Same(hubConnection, capturedContext.HubConnection);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncOnRefreshedCallbackExceptionIsSwallowed()
        {
            bool ExpectedErrors(WriteContext wc) =>
                wc.LoggerName == typeof(HubConnection).FullName &&
                wc.EventId.Name == "AuthRefreshCallbackFailed";

            using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
            {
                var feature = new FakeAuthRefreshFeature { NextTtlSeconds = 1200 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o =>
                    {
                        o.EnableAutoRefresh = false;
                        o.OnRefreshed = _ => throw new InvalidOperationException("callback boom");
                    });
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    // Should NOT throw - callback exception is logged & swallowed.
                    var ttl = await hubConnection.RefreshAuthAsync().DefaultTimeout();
                    Assert.Equal(1200, ttl);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshAuthAsyncOnRefreshFailedCallbackExceptionIsSwallowedAndOriginalRethrown()
        {
            bool ExpectedErrors(WriteContext wc) =>
                wc.LoggerName == typeof(HubConnection).FullName &&
                wc.EventId.Name == "AuthRefreshCallbackFailed";

            using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
            {
                var original = new InvalidOperationException("refresh boom");
                var feature = new FakeAuthRefreshFeature { ExceptionToThrow = original };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o =>
                    {
                        o.EnableAutoRefresh = false;
                        o.OnRefreshFailed = _ => throw new InvalidOperationException("failed-callback boom");
                    });
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => hubConnection.RefreshAuthAsync()).DefaultTimeout();

                    Assert.Same(original, ex);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartSchedulesTimerWhenInitialTtlProvided()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.NotNull(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleTimerWhenEnableAutoRefreshFalse()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.EnableAutoRefresh = false);
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleTimerWhenFeatureMissing()
        {
            using (StartVerifiableLog())
            {
                var connection = new TestConnection();
                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleTimerWhenInitialTtlIsZero()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = 0 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleTimerWhenInitialTtlIsNull()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = null };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshReschedulesTimerWhenAutoRefreshEnabled()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature
                {
                    InitialTokenLifetimeSeconds = 3600,
                    NextTtlSeconds = 7200,
                };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();
                    var beforeTimer = GetAuthRefreshTimer(hubConnection);
                    Assert.NotNull(beforeTimer);

                    await hubConnection.RefreshAuthAsync().DefaultTimeout();
                    var afterTimer = GetAuthRefreshTimer(hubConnection);

                    Assert.NotNull(afterTimer);
                    Assert.NotSame(beforeTimer, afterTimer);
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshDoesNotRescheduleTimerWhenAutoRefreshDisabled()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { NextTtlSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.EnableAutoRefresh = false);
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();
                    Assert.Null(GetAuthRefreshTimer(hubConnection));

                    await hubConnection.RefreshAuthAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartSchedulesFallbackTimerWhenNoInitialTtlButFallbackIntervalSet()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = null };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromMinutes(20));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.NotNull(GetAuthRefreshTimer(hubConnection));
                    Assert.Equal(TimeSpan.FromMinutes(20), GetLastAuthRefreshDelay(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartIgnoresFallbackIntervalWhenServerReportsTtl()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromMinutes(20));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.NotNull(GetAuthRefreshTimer(hubConnection));
                    // Server TTL (3600s) minus the default 5 minute lead time wins over the 20 minute fallback.
                    Assert.Equal(TimeSpan.FromSeconds(3600) - TimeSpan.FromMinutes(5), GetLastAuthRefreshDelay(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartFloorsFallbackIntervalToMinimum()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = null };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromSeconds(5));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.NotNull(GetAuthRefreshTimer(hubConnection));
                    Assert.Equal(TimeSpan.FromSeconds(30), GetLastAuthRefreshDelay(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleFallbackTimerWhenFeatureMissing()
        {
            using (StartVerifiableLog())
            {
                var connection = new TestConnection();
                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromMinutes(20));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task StartDoesNotScheduleFallbackTimerWhenAutoRefreshDisabled()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = null };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o =>
                    {
                        o.EnableAutoRefresh = false;
                        o.FallbackRefreshInterval = TimeSpan.FromMinutes(20);
                    });
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    Assert.Null(GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshReschedulesFallbackTimerWhenServerReturnsNoTtl()
        {
            using (StartVerifiableLog())
            {
                // Server never reports a token lifetime, so the fallback interval must keep the timer armed
                // across refreshes rather than firing once and stopping.
                var feature = new FakeAuthRefreshFeature
                {
                    InitialTokenLifetimeSeconds = null,
                    NextTtlSeconds = null,
                };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromMinutes(20));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();
                    var beforeTimer = GetAuthRefreshTimer(hubConnection);
                    Assert.NotNull(beforeTimer);

                    await hubConnection.RefreshAuthAsync().DefaultTimeout();
                    var afterTimer = GetAuthRefreshTimer(hubConnection);

                    Assert.NotNull(afterTimer);
                    Assert.NotSame(beforeTimer, afterTimer);
                    Assert.Equal(TimeSpan.FromMinutes(20), GetLastAuthRefreshDelay(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshDoesNotRescheduleWhenServerReturnsNoTtlAndNoFallback()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature
                {
                    InitialTokenLifetimeSeconds = 3600,
                    NextTtlSeconds = null,
                };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();
                    var beforeTimer = GetAuthRefreshTimer(hubConnection);
                    Assert.NotNull(beforeTimer);

                    await hubConnection.RefreshAuthAsync().DefaultTimeout();

                    // No new TTL and no fallback interval means the timer is not re-armed; the existing
                    // one-shot timer is left in place (and will simply not fire again after it elapses).
                    Assert.Same(beforeTimer, GetAuthRefreshTimer(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task RefreshUsesServerTtlOverFallbackIntervalWhenRescheduling()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature
                {
                    InitialTokenLifetimeSeconds = null,
                    NextTtlSeconds = 7200,
                };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection, builder =>
                {
                    builder.WithAuthRefresh(o => o.FallbackRefreshInterval = TimeSpan.FromMinutes(20));
                });
                try
                {
                    await hubConnection.StartAsync().DefaultTimeout();

                    await hubConnection.RefreshAuthAsync().DefaultTimeout();

                    // The refresh returned a server TTL (7200s), which wins over the 20 minute fallback.
                    Assert.Equal(TimeSpan.FromSeconds(7200) - TimeSpan.FromMinutes(5), GetLastAuthRefreshDelay(hubConnection));
                }
                finally
                {
                    await hubConnection.DisposeAsync().DefaultTimeout();
                    await connection.DisposeAsync().DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task DisposeDisposesAuthRefreshTimer()
        {
            using (StartVerifiableLog())
            {
                var feature = new FakeAuthRefreshFeature { InitialTokenLifetimeSeconds = 3600 };
                var connection = new TestConnection();
                connection.Features.Set<IAuthRefreshFeature>(feature);

                var hubConnection = BuildHubConnection(connection);
                await hubConnection.StartAsync().DefaultTimeout();
                Assert.NotNull(GetAuthRefreshTimer(hubConnection));

                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();

                Assert.Null(GetAuthRefreshTimer(hubConnection));
            }
        }

        [Fact]
        public void WithAuthRefreshRegistersOptionsThroughDI()
        {
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");
            builder.WithAuthRefresh(o =>
            {
                o.EnableAutoRefresh = false;
                o.RefreshBeforeExpiration = TimeSpan.FromMinutes(7);
            });

            var sp = builder.Services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<AuthRefreshOptions>>().Value;

            Assert.False(options.EnableAutoRefresh);
            Assert.Equal(TimeSpan.FromMinutes(7), options.RefreshBeforeExpiration);
        }

        [Fact]
        public void WithAuthRefreshThrowsOnNullConfigure()
        {
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");
            Assert.Throws<ArgumentNullException>(() => builder.WithAuthRefresh(null));
        }

        private sealed class FakeAuthRefreshFeature : IAuthRefreshFeature
        {
            public int? InitialTokenLifetimeSeconds { get; set; }
            public int? NextTtlSeconds { get; set; }
            public Exception ExceptionToThrow { get; set; }
            public int RefreshCallCount { get; private set; }

            public Task<int?> RefreshAuthAsync(CancellationToken cancellationToken = default)
            {
                RefreshCallCount++;
                if (ExceptionToThrow is not null)
                {
                    return Task.FromException<int?>(ExceptionToThrow);
                }
                return Task.FromResult(NextTtlSeconds);
            }
        }
    }
}
