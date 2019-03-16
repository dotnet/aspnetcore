// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class DefaultHttpClientFactoryTest
    {
        public DefaultHttpClientFactoryTest()
        {
            Services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            ScopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            Options = Services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        }

        public IServiceProvider Services { get; }

        public IServiceScopeFactory ScopeFactory { get; }

        public ILoggerFactory LoggerFactory { get; } = NullLoggerFactory.Instance;

        public IOptionsMonitor<HttpClientFactoryOptions> Options { get; }

        public IEnumerable<IHttpMessageHandlerBuilderFilter> EmptyFilters = Array.Empty<IHttpMessageHandlerBuilderFilter>();

        [Fact]
        public void Factory_MultipleCalls_DoesNotCacheHttpClient()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpClientActions.Add(c =>
            {
                count++;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act 1
            var client1 = factory.CreateClient();

            // Act 2
            var client2 = factory.CreateClient();

            // Assert
            Assert.Equal(2, count);
            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void Factory_MultipleCalls_CachesHandler()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpMessageHandlerBuilderActions.Add(b =>
            {
                count++;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act 1
            var client1 = factory.CreateClient();

            // Act 2
            var client2 = factory.CreateClient();

            // Assert
            Assert.Equal(1, count);
            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void Factory_DisposeClient_DoesNotDisposeHandler()
        {
            // Arrange
            Options.CurrentValue.HttpMessageHandlerBuilderActions.Add(b =>
            {
                var mockHandler = new Mock<HttpMessageHandler>();
                mockHandler
                    .Protected()
                    .Setup("Dispose", true)
                    .Throws(new Exception("Dispose should not be called"));

                b.PrimaryHandler = mockHandler.Object;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act
            using (factory.CreateClient())
            {
            }

            // Assert (does not throw)
        }

        [Fact]
        public void Factory_DisposeHandler_DoesNotDisposeInnerHandler()
        {
            // Arrange
            Options.CurrentValue.HttpMessageHandlerBuilderActions.Add(b =>
            {
                var mockHandler = new Mock<HttpMessageHandler>();
                mockHandler
                    .Protected()
                    .Setup("Dispose", true)
                    .Throws(new Exception("Dispose should not be called"));

                b.PrimaryHandler = mockHandler.Object;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act
            using (factory.CreateHandler())
            {
            }

            // Assert (does not throw)
        }

        [Fact]
        public void Factory_CreateClient_WithoutName_UsesDefaultOptions()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpClientActions.Add(b =>
            {
                count++;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act
            var client = factory.CreateClient();

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void Factory_CreateClient_WithName_UsesNamedOptions()
        {
            // Arrange
            var count = 0;
            Options.Get("github").HttpClientActions.Add(b =>
            {
                count++;
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters);

            // Act
            var client = factory.CreateClient("github");

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void Factory_CreateClient_FiltersCanDecorateBuilder()
        {
            // Arrange
            var expected = new HttpMessageHandler[]
            {
                Mock.Of<DelegatingHandler>(), // Added by filter1
                Mock.Of<DelegatingHandler>(), // Added by filter2
                Mock.Of<DelegatingHandler>(), // Added by filter3
                Mock.Of<DelegatingHandler>(), // Added in options
                Mock.Of<DelegatingHandler>(), // Added by filter3
                Mock.Of<DelegatingHandler>(), // Added by filter2
                Mock.Of<DelegatingHandler>(), // Added by filter1

                Mock.Of<HttpMessageHandler>(), // Set as primary handler by options
            };

            Options.Get("github").HttpMessageHandlerBuilderActions.Add(b =>
            {
                b.PrimaryHandler = expected[7];

                b.AdditionalHandlers.Add((DelegatingHandler)expected[3]);
            });

            var filter1 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter1
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) =>
                {
                    next(b); // Calls filter2
                    b.AdditionalHandlers.Insert(0, (DelegatingHandler)expected[0]);
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[6]);
                });

            var filter2 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter2
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) =>
                {
                    next(b); // Calls filter3
                    b.AdditionalHandlers.Insert(0, (DelegatingHandler)expected[1]);
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[5]);
                });

            var filter3 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter3
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) =>
                {
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[2]);
                    next(b); // Calls options
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[4]);
                });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, new[]
            {
                filter1.Object,
                filter2.Object,
                filter3.Object,
            });

            // Act
            var handler = (HttpMessageHandler)factory.CreateHandlerEntry("github").Handler;

            // Assert
            //
            // The outer-most handler is always a lifetime tracking handler.
            Assert.IsType<LifetimeTrackingHttpMessageHandler>(handler);
            handler = Assert.IsAssignableFrom<DelegatingHandler>(handler).InnerHandler;

            for (var i = 0; i < expected.Length - 1; i++)
            {
                Assert.Same(expected[i], handler);
                handler = Assert.IsAssignableFrom<DelegatingHandler>(handler).InnerHandler;
            }

            Assert.Same(expected[7], handler);
        }

        [Fact]
        public async Task Factory_CreateClient_WithExpiry_CanExpire()
        {
            // Arrange
            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters)
            {
                EnableExpiryTimer = true,
                EnableCleanupTimer = true,
            };

            // Act - 1 - Creating a client should add an entry to active handlers
            var client1 = factory.CreateClient("github");

            // Assert - 1
            var activeEntry1 = Assert.Single(factory._activeHandlers).Value.Value;
            Assert.Equal("github", activeEntry1.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), activeEntry1.Lifetime);
            Assert.NotNull(activeEntry1.Handler);

            // Act - 2 - Now simulate the timer triggering to complete the expiry.
            var (completionSource, expiryTask) = factory.ActiveEntryState[activeEntry1];
            completionSource.SetResult(activeEntry1);
            await expiryTask;

            // Assert - 2
            Assert.Empty(factory._activeHandlers);
            Assert.True(factory.CleanupTimerStarted.IsSet, "Cleanup timer started");

            var expiredEntry1 = Assert.Single(factory._expiredHandlers);
            Assert.NotSame(expiredEntry1.InnerHandler, activeEntry1.Handler);

            // Act - 3 - Creating a client should add another entry
            var client2 = factory.CreateClient("github");

            // Assert - 3
            var activeEntry2 = Assert.Single(factory._activeHandlers).Value.Value;
            Assert.Equal("github", activeEntry1.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), activeEntry1.Lifetime);
            Assert.NotNull(activeEntry1.Handler);
            Assert.NotSame(activeEntry1, activeEntry2);
            Assert.NotSame(activeEntry1.Handler, activeEntry2.Handler);
        }

        [Fact]
        public async Task Factory_CreateClient_WithExpiry_HandlerCanBeReusedBeforeExpiry()
        {
            // Arrange
            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters)
            {
                EnableExpiryTimer = true,
                EnableCleanupTimer = true,
            };

            // Act - 1 - Creating a client should add an entry to active handlers
            var client1 = factory.CreateClient("github");

            // Assert - 1
            var activeEntry1 = Assert.Single(factory._activeHandlers).Value.Value;
            Assert.Equal("github", activeEntry1.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), activeEntry1.Lifetime);
            Assert.NotNull(activeEntry1.Handler);

            // Act - 2 - Now create another client, it shouldn't replace the entry.
            var client2 = factory.CreateClient("github");

            // Assert - 2
            Assert.Same(activeEntry1, Assert.Single(factory._activeHandlers).Value.Value);

            // Act - 3 - Now simulate the timer triggering to complete the expiry.
            var (completionSource, expiryTask) = factory.ActiveEntryState[activeEntry1];
            completionSource.SetResult(activeEntry1);
            await expiryTask;

            // Assert - 3
            Assert.Empty(factory._activeHandlers);
            Assert.True(factory.CleanupTimerStarted.IsSet, "Cleanup timer started");

            var expiredEntry1 = Assert.Single(factory._expiredHandlers);
            Assert.NotSame(expiredEntry1.InnerHandler, activeEntry1.Handler);

            // Act - 4 - Creating a client should add another entry
            var client3 = factory.CreateClient("github");

            // Assert - 4
            var activeEntry2 = Assert.Single(factory._activeHandlers).Value.Value;
            Assert.Equal("github", activeEntry1.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), activeEntry1.Lifetime);
            Assert.NotNull(activeEntry1.Handler);
            Assert.NotSame(activeEntry1, activeEntry2);
            Assert.NotSame(activeEntry1.Handler, activeEntry2.Handler);
        }

        [Fact]
        public async Task Factory_CleanupCycle_DisposesEligibleHandler()
        {
            // Arrange
            var disposeHandler = new DisposeTrackingHandler();
            Options.Get("github").HttpMessageHandlerBuilderActions.Add(b =>
            {
                b.AdditionalHandlers.Add(disposeHandler);
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters)
            {
                EnableExpiryTimer = true,
                EnableCleanupTimer = true,
            };

            var cleanupEntry = await SimulateClientUse_Factory_CleanupCycle_DisposesEligibleHandler(factory);

            // Being pretty conservative here because we want this test to be reliable,
            // and it depends on the GC and timing.
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (cleanupEntry.CanDispose)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Assert.True(cleanupEntry.CanDispose, "Cleanup entry disposable");

            // Act
            factory.CleanupTimer_Tick();

            // Assert
            Assert.Empty(factory._expiredHandlers);
            Assert.Equal(1, disposeHandler.DisposeCount);
            Assert.False(factory.CleanupTimerStarted.IsSet, "Cleanup timer not started");
        }

        // Seprate to avoid the HttpClient getting its lifetime extended by
        // the state machine of the test.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<ExpiredHandlerTrackingEntry> SimulateClientUse_Factory_CleanupCycle_DisposesEligibleHandler(TestHttpClientFactory factory)
        {
            // Create a handler and move it to the expired state
            var client1 = factory.CreateClient("github");

            var kvp = Assert.Single(factory.ActiveEntryState);
            kvp.Value.Item1.SetResult(kvp.Key);
            await kvp.Value.Item2;

            // Our handler is now in the cleanup state.
            var cleanupEntry = Assert.Single(factory._expiredHandlers);
            Assert.True(factory.CleanupTimerStarted.IsSet, "Cleanup timer started");

            // We need to make sure that the outer handler actually gets GCed, so drop our references to it.
            // This is important because the factory relies on this possibility for correctness. We need to ensure that
            // the factory isn't keeping any references.
            kvp = default;
            client1 = null;

            return cleanupEntry;
        }

        [Fact]
        public async Task Factory_CleanupCycle_DisposesLiveHandler()
        {
            // Arrange
            var disposeHandler = new DisposeTrackingHandler();
            Options.Get("github").HttpMessageHandlerBuilderActions.Add(b =>
            {
                b.AdditionalHandlers.Add(disposeHandler);
            });

            var factory = new TestHttpClientFactory(Services, ScopeFactory, LoggerFactory, Options, EmptyFilters)
            {
                EnableExpiryTimer = true,
                EnableCleanupTimer = true,
            };

            var cleanupEntry = await SimulateClientUse_Factory_CleanupCycle_DisposesLiveHandler(factory, disposeHandler);

            // Being pretty conservative here because we want this test to be reliable,
            // and it depends on the GC and timing.
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (cleanupEntry.CanDispose)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Assert.True(cleanupEntry.CanDispose, "Cleanup entry disposable");

            // Act - 2
            factory.CleanupTimer_Tick();

            // Assert
            Assert.Empty(factory._expiredHandlers);
            Assert.Equal(1, disposeHandler.DisposeCount);
            Assert.False(factory.CleanupTimerStarted.IsSet, "Cleanup timer not started");
        }

        // Seprate to avoid the HttpClient getting its lifetime extended by
        // the state machine of the test.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<ExpiredHandlerTrackingEntry> SimulateClientUse_Factory_CleanupCycle_DisposesLiveHandler(
            TestHttpClientFactory factory,
            DisposeTrackingHandler disposeHandler)
        {
            // Create a handler and move it to the expired state
            var client1 = factory.CreateClient("github");

            var kvp = Assert.Single(factory.ActiveEntryState);
            kvp.Value.Item1.SetResult(kvp.Key);
            await kvp.Value.Item2;

            // Our handler is now in the cleanup state.
            var cleanupEntry = Assert.Single(factory._expiredHandlers);
            Assert.True(factory.CleanupTimerStarted.IsSet, "Cleanup timer started");

            // Nulling out the references to the internal state of the factory since they wouldn't exist in the non-test
            // scenario. We're holding on the client to prevent disposal - like a real use case.
            lock (this)
            {
                // Prevent reordering
                kvp = default;
            }

            // Let's verify the the ActiveHandlerTrackingEntry is gone. This would be prevent
            // the handler from being disposed if it was still rooted.
            Assert.Empty(factory.ActiveEntryState);

            // Act - 1 - Run a cleanup cycle, this will not dispose the handler, because the client is still live.
            factory.CleanupTimer_Tick();

            // Assert
            Assert.Same(cleanupEntry, Assert.Single(factory._expiredHandlers));
            Assert.Equal(0, disposeHandler.DisposeCount);
            Assert.True(factory.CleanupTimerStarted.IsSet, "Cleanup timer started");

            // We need to make sure that the outer handler actually gets GCed, so drop our references to it.
            // This is important because the factory relies on this possibility for correctness. We need to ensure that
            // the factory isn't keeping any references.
            lock (this)
            {
                // Prevent reordering
                GC.KeepAlive(client1);
                client1 = null;
            }

            return cleanupEntry;
        }

        private class TestHttpClientFactory : DefaultHttpClientFactory
        {
            public TestHttpClientFactory(
                IServiceProvider services,
                IServiceScopeFactory scopeFactory,
                ILoggerFactory loggerFactory,
                IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
                IEnumerable<IHttpMessageHandlerBuilderFilter> filters)
                : base(services, scopeFactory, loggerFactory, optionsMonitor, filters)
            {
                ActiveEntryState = new Dictionary<ActiveHandlerTrackingEntry, (TaskCompletionSource<ActiveHandlerTrackingEntry>, Task)>();
                CleanupTimerStarted = new ManualResetEventSlim(initialState: false);
            }

            public bool EnableExpiryTimer { get; set; }

            public bool EnableCleanupTimer { get; set; }

            public ManualResetEventSlim CleanupTimerStarted { get; }

            public Dictionary<ActiveHandlerTrackingEntry, (TaskCompletionSource<ActiveHandlerTrackingEntry>, Task)> ActiveEntryState { get; }

            internal override void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
            {
                if (EnableExpiryTimer)
                {
                    lock (ActiveEntryState)
                    {
                        if (ActiveEntryState.ContainsKey(entry))
                        {
                            // Timer already started.
                            return;
                        }

                        // Rather than using the actual timer on the actual entry, let's fake it with async.
                        var completionSource = new TaskCompletionSource<ActiveHandlerTrackingEntry>();
                        var expiryTask = completionSource.Task.ContinueWith(t =>
                        {
                            var e = t.Result;
                            ExpiryTimer_Tick(e);

                            lock (ActiveEntryState)
                            {
                                ActiveEntryState.Remove(e);
                            }
                        });

                        ActiveEntryState.Add(entry, (completionSource, expiryTask));
                    }
                }
            }

            internal override void StartCleanupTimer()
            {
                if (EnableCleanupTimer)
                {
                    CleanupTimerStarted.Set();
                }
            }

            internal override void StopCleanupTimer()
            {
                if (EnableCleanupTimer)
                {
                    Assert.True(CleanupTimerStarted.IsSet, "Cleanup timer started");
                    CleanupTimerStarted.Reset();
                }
            }
        }

        private class DisposeTrackingHandler : DelegatingHandler
        {
            public int DisposeCount { get; set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
            }
        }
    }
}
