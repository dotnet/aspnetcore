// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public class KeyRingProviderTests
    {
        [Fact]
        public void CreateCacheableKeyRing_NoGenerationRequired_DefaultKeyExpiresAfterRefreshPeriod()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts = new CancellationTokenSource();

            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
            var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");
            var allKeys = new[] { key1, key2 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts.Token },
                getAllKeysReturnValues: new[] { allKeys },
                createNewKeyCallbacks: null,
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = key1,
                            ShouldGenerateNewKey = false
                        })
                });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_NoGenerationRequired_DefaultKeyExpiresBeforeRefreshPeriod()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts = new CancellationTokenSource();

            var now = StringToDateTime("2016-02-29 20:00:00Z");
            var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
            var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");
            var allKeys = new[] { key1, key2 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts.Token },
                getAllKeysReturnValues: new[] { allKeys },
                createNewKeyCallbacks: null,
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = key1,
                            ShouldGenerateNewKey = false
                        })
                });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            Assert.Equal(StringToDateTime("2016-03-01 00:00:00Z"), cacheableKeyRing.ExpirationTimeUtc);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_NoDefaultKey_CreatesNewKeyWithImmediateActivation()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts1 = new CancellationTokenSource();
            var expirationCts2 = new CancellationTokenSource();

            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var allKeys1 = new IKey[0];

            var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
            var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");
            var allKeys2 = new[] { key1, key2 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts1.Token, expirationCts2.Token },
                getAllKeysReturnValues: new[] { allKeys1, allKeys2 },
                createNewKeyCallbacks: new[] {
                    Tuple.Create((DateTimeOffset)now, (DateTimeOffset)now + TimeSpan.FromDays(90), CreateKey())
                },
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys1, new DefaultKeyResolution()
                        {
                            DefaultKey = null,
                            ShouldGenerateNewKey = true
                        }),
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys2, new DefaultKeyResolution()
                        {
                            DefaultKey = key1,
                            ShouldGenerateNewKey = false
                        })
                });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts1.Cancel();
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts2.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy", "CreateNewKey", "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_NoDefaultKey_CreatesNewKeyWithImmediateActivation_StillNoDefaultKey_ReturnsNewlyCreatedKey()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts1 = new CancellationTokenSource();
            var expirationCts2 = new CancellationTokenSource();

            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var allKeys = new IKey[0];

            var newlyCreatedKey = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts1.Token, expirationCts2.Token },
                getAllKeysReturnValues: new[] { allKeys, allKeys },
                createNewKeyCallbacks: new[] {
                    Tuple.Create((DateTimeOffset)now, (DateTimeOffset)now + TimeSpan.FromDays(90), newlyCreatedKey)
                },
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = null,
                            ShouldGenerateNewKey = true
                        }),
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = null,
                            ShouldGenerateNewKey = true
                        })
                });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(newlyCreatedKey.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts1.Cancel();
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts2.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy", "CreateNewKey", "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_NoDefaultKey_KeyGenerationDisabled_Fails()
        {
            // Arrange
            var callSequence = new List<string>();

            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var allKeys = new IKey[0];

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { CancellationToken.None },
                getAllKeysReturnValues: new[] { allKeys },
                createNewKeyCallbacks: new[] {
                    Tuple.Create((DateTimeOffset)now, (DateTimeOffset)now + TimeSpan.FromDays(90), CreateKey())
                },
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = null,
                            ShouldGenerateNewKey = true
                        })
                },
                keyManagementOptions: new KeyManagementOptions() { AutoGenerateKeys = false });

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => keyRingProvider.GetCacheableKeyRing(now));

            // Assert
            Assert.Equal(Resources.KeyRingProvider_NoDefaultKey_AutoGenerateDisabled, exception.Message);
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_WithDefaultKey_CreatesNewKeyWithDeferredActivationAndExpirationBasedOnCreationTime()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts1 = new CancellationTokenSource();
            var expirationCts2 = new CancellationTokenSource();

            var now = StringToDateTime("2016-02-01 00:00:00Z");
            var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
            var allKeys1 = new[] { key1 };

            var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");
            var allKeys2 = new[] { key1, key2 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts1.Token, expirationCts2.Token },
                getAllKeysReturnValues: new[] { allKeys1, allKeys2 },
                createNewKeyCallbacks: new[] {
                    Tuple.Create(key1.ExpirationDate, (DateTimeOffset)now + TimeSpan.FromDays(90), CreateKey())
                },
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys1, new DefaultKeyResolution()
                        {
                            DefaultKey = key1,
                            ShouldGenerateNewKey = true
                        }),
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys2, new DefaultKeyResolution()
                        {
                            DefaultKey = key2,
                            ShouldGenerateNewKey = false
                        })
                });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key2.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts1.Cancel();
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts2.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy", "CreateNewKey", "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_WithDefaultKey_KeyGenerationDisabled_DoesNotCreateDefaultKey()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts = new CancellationTokenSource();

            var now = StringToDateTime("2016-02-01 00:00:00Z");
            var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
            var allKeys = new[] { key1 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts.Token },
                getAllKeysReturnValues: new[] { allKeys },
                createNewKeyCallbacks: null, // empty
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            DefaultKey = key1,
                            ShouldGenerateNewKey = true
                        })
                },
                keyManagementOptions: new KeyManagementOptions() { AutoGenerateKeys = false });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void CreateCacheableKeyRing_GenerationRequired_WithFallbackKey_KeyGenerationDisabled_DoesNotCreateDefaultKey()
        {
            // Arrange
            var callSequence = new List<string>();
            var expirationCts = new CancellationTokenSource();

            var now = StringToDateTime("2016-02-01 00:00:00Z");
            var key1 = CreateKey("2015-03-01 00:00:00Z", "2015-03-01 00:00:00Z");
            var allKeys = new[] { key1 };

            var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
                callSequence: callSequence,
                getCacheExpirationTokenReturnValues: new[] { expirationCts.Token },
                getAllKeysReturnValues: new[] { allKeys },
                createNewKeyCallbacks: null, // empty
                resolveDefaultKeyPolicyReturnValues: new[]
                {
                        Tuple.Create((DateTimeOffset)now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                        {
                            FallbackKey = key1,
                            ShouldGenerateNewKey = true
                        })
                },
                keyManagementOptions: new KeyManagementOptions() { AutoGenerateKeys = false });

            // Act
            var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

            // Assert
            Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
            AssertWithinJitterRange(cacheableKeyRing.ExpirationTimeUtc, now);
            Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            expirationCts.Cancel();
            Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
            Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" }, callSequence);
        }

        [Fact]
        public void GetCurrentKeyRing_NoKeyRingCached_CachesAndReturns()
        {
            // Arrange
            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var expectedKeyRing = new Mock<IKeyRing>().Object;
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-02 00:00:00Z"),
                    keyRing: expectedKeyRing));

            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // Act
            var retVal1 = keyRingProvider.GetCurrentKeyRingCore(now);
            var retVal2 = keyRingProvider.GetCurrentKeyRingCore(now + TimeSpan.FromHours(1));

            // Assert - underlying provider only should have been called once
            Assert.Same(expectedKeyRing, retVal1);
            Assert.Same(expectedKeyRing, retVal2);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(It.IsAny<DateTimeOffset>()), Times.Once);
        }

        [Fact]
        public void GetCurrentKeyRing_KeyRingCached_CanForceRefresh()
        {
            // Arrange
            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var expectedKeyRing1 = new Mock<IKeyRing>().Object;
            var expectedKeyRing2 = new Mock<IKeyRing>().Object;
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-01 00:30:00Z"), // expire in half an hour
                    keyRing: expectedKeyRing1));
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now + TimeSpan.FromMinutes(1)))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-01 00:30:00Z"), // expire in half an hour
                    keyRing: expectedKeyRing1));
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now + TimeSpan.FromMinutes(2)))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-02 00:00:00Z"),
                    keyRing: expectedKeyRing2));

            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // Act
            var retVal1 = keyRingProvider.GetCurrentKeyRingCore(now);
            var retVal2 = keyRingProvider.GetCurrentKeyRingCore(now + TimeSpan.FromMinutes(1));
            var retVal3 = keyRingProvider.GetCurrentKeyRingCore(now + TimeSpan.FromMinutes(2), forceRefresh: true);

            // Assert - underlying provider should be called twice
            Assert.Same(expectedKeyRing1, retVal1);
            Assert.Same(expectedKeyRing1, retVal2);
            Assert.Same(expectedKeyRing2, retVal3);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(It.IsAny<DateTimeOffset>()), Times.Exactly(2));
        }

        [Fact]
        public void GetCurrentKeyRing_KeyRingCached_AfterExpiration_ClearsCache()
        {
            // Arrange
            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var expectedKeyRing1 = new Mock<IKeyRing>().Object;
            var expectedKeyRing2 = new Mock<IKeyRing>().Object;
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-01 00:30:00Z"), // expire in half an hour
                    keyRing: expectedKeyRing1));
            mockCacheableKeyRingProvider
                .Setup(o => o.GetCacheableKeyRing(now + TimeSpan.FromHours(1)))
                .Returns(new CacheableKeyRing(
                    expirationToken: CancellationToken.None,
                    expirationTime: StringToDateTime("2015-03-02 00:00:00Z"),
                    keyRing: expectedKeyRing2));

            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // Act
            var retVal1 = keyRingProvider.GetCurrentKeyRingCore(now);
            var retVal2 = keyRingProvider.GetCurrentKeyRingCore(now + TimeSpan.FromHours(1));

            // Assert - underlying provider only should have been called once
            Assert.Same(expectedKeyRing1, retVal1);
            Assert.Same(expectedKeyRing2, retVal2);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(It.IsAny<DateTimeOffset>()), Times.Exactly(2));
        }

        [Fact]
        public void GetCurrentKeyRing_NoExistingKeyRing_HoldsAllThreadsUntilKeyRingCreated()
        {
            // Arrange
            var now = StringToDateTime("2015-03-01 00:00:00Z");
            var expectedKeyRing = new Mock<IKeyRing>().Object;
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // This test spawns a background thread which calls GetCurrentKeyRing then waits
            // for the foreground thread to call GetCurrentKeyRing. When the foreground thread
            // blocks (inside the lock), the background thread will return the cached keyring
            // object, and the foreground thread should consume that same object instance.

            TimeSpan testTimeout = TimeSpan.FromSeconds(10);

            Thread foregroundThread = Thread.CurrentThread;
            ManualResetEventSlim mreBackgroundThreadHasCalledGetCurrentKeyRing = new ManualResetEventSlim();
            ManualResetEventSlim mreForegroundThreadIsCallingGetCurrentKeyRing = new ManualResetEventSlim();
            var backgroundGetKeyRingTask = Task.Run(() =>
            {
                mockCacheableKeyRingProvider
                    .Setup(o => o.GetCacheableKeyRing(now))
                    .Returns(() =>
                    {
                        mreBackgroundThreadHasCalledGetCurrentKeyRing.Set();
                        Assert.True(mreForegroundThreadIsCallingGetCurrentKeyRing.Wait(testTimeout), "Test timed out.");
                        SpinWait.SpinUntil(() => (foregroundThread.ThreadState & ThreadState.WaitSleepJoin) != 0, testTimeout);
                        return new CacheableKeyRing(
                            expirationToken: CancellationToken.None,
                            expirationTime: StringToDateTime("2015-03-02 00:00:00Z"),
                            keyRing: expectedKeyRing);
                    });

                return keyRingProvider.GetCurrentKeyRingCore(now);
            });

            Assert.True(mreBackgroundThreadHasCalledGetCurrentKeyRing.Wait(testTimeout), "Test timed out.");
            mreForegroundThreadIsCallingGetCurrentKeyRing.Set();
            var foregroundRetVal = keyRingProvider.GetCurrentKeyRingCore(now);
            backgroundGetKeyRingTask.Wait(testTimeout);
            var backgroundRetVal = backgroundGetKeyRingTask.GetAwaiter().GetResult();

            // Assert - underlying provider only should have been called once
            Assert.Same(expectedKeyRing, foregroundRetVal);
            Assert.Same(expectedKeyRing, backgroundRetVal);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(It.IsAny<DateTimeOffset>()), Times.Once);
        }

        [Fact]
        public void GetCurrentKeyRing_WithExpiredExistingKeyRing_AllowsOneThreadToUpdate_ReturnsExistingKeyRingToOtherCallersWithoutBlocking()
        {
            // Arrange
            var originalKeyRing = new Mock<IKeyRing>().Object;
            var originalKeyRingTime = StringToDateTime("2015-03-01 00:00:00Z");
            var updatedKeyRing = new Mock<IKeyRing>().Object;
            var updatedKeyRingTime = StringToDateTime("2015-03-02 00:00:00Z");
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // In this test, the foreground thread acquires the critial section in GetCurrentKeyRing,
            // and the background thread returns the original key ring rather than blocking while
            // waiting for the foreground thread to update the key ring.

            TimeSpan testTimeout = TimeSpan.FromSeconds(10);
            IKeyRing keyRingReturnedToBackgroundThread = null;

            mockCacheableKeyRingProvider.Setup(o => o.GetCacheableKeyRing(originalKeyRingTime))
                .Returns(new CacheableKeyRing(CancellationToken.None, StringToDateTime("2015-03-02 00:00:00Z"), originalKeyRing));
            mockCacheableKeyRingProvider.Setup(o => o.GetCacheableKeyRing(updatedKeyRingTime))
                .Returns<DateTimeOffset>(dto =>
                {
                    // at this point we're inside the critical section - spawn the background thread now
                    var backgroundGetKeyRingTask = Task.Run(() =>
                    {
                        keyRingReturnedToBackgroundThread = keyRingProvider.GetCurrentKeyRingCore(updatedKeyRingTime);
                    });
                    Assert.True(backgroundGetKeyRingTask.Wait(testTimeout), "Test timed out.");

                    return new CacheableKeyRing(CancellationToken.None, StringToDateTime("2015-03-03 00:00:00Z"), updatedKeyRing);
                });

            // Assert - underlying provider only should have been called once with the updated time (by the foreground thread)
            Assert.Same(originalKeyRing, keyRingProvider.GetCurrentKeyRingCore(originalKeyRingTime));
            Assert.Same(updatedKeyRing, keyRingProvider.GetCurrentKeyRingCore(updatedKeyRingTime));
            Assert.Same(originalKeyRing, keyRingReturnedToBackgroundThread);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(updatedKeyRingTime), Times.Once);
        }

        [Fact]
        public void GetCurrentKeyRing_WithExpiredExistingKeyRing_UpdateFails_ThrowsButCachesOldKeyRing()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
            var originalKeyRing = new Mock<IKeyRing>().Object;
            var originalKeyRingTime = StringToDateTime("2015-03-01 00:00:00Z");
            mockCacheableKeyRingProvider.Setup(o => o.GetCacheableKeyRing(originalKeyRingTime))
                .Returns(new CacheableKeyRing(cts.Token, StringToDateTime("2015-03-02 00:00:00Z"), originalKeyRing));
            var throwKeyRingTime = StringToDateTime("2015-03-01 12:00:00Z");
            mockCacheableKeyRingProvider.Setup(o => o.GetCacheableKeyRing(throwKeyRingTime)).Throws(new Exception("How exceptional."));
            var updatedKeyRing = new Mock<IKeyRing>().Object;
            var updatedKeyRingTime = StringToDateTime("2015-03-01 12:02:00Z");
            mockCacheableKeyRingProvider.Setup(o => o.GetCacheableKeyRing(updatedKeyRingTime))
                .Returns(new CacheableKeyRing(CancellationToken.None, StringToDateTime("2015-03-02 00:00:00Z"), updatedKeyRing));
            var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

            // Act & assert
            Assert.Same(originalKeyRing, keyRingProvider.GetCurrentKeyRingCore(originalKeyRingTime));
            cts.Cancel(); // invalidate the key ring
            ExceptionAssert.Throws<Exception>(() => keyRingProvider.GetCurrentKeyRingCore(throwKeyRingTime), "How exceptional.");
            Assert.Same(originalKeyRing, keyRingProvider.GetCurrentKeyRingCore(throwKeyRingTime));
            Assert.Same(updatedKeyRing, keyRingProvider.GetCurrentKeyRingCore(updatedKeyRingTime));
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(originalKeyRingTime), Times.Once);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(throwKeyRingTime), Times.Once);
            mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(updatedKeyRingTime), Times.Once);
        }

        private static ICacheableKeyRingProvider SetupCreateCacheableKeyRingTestAndCreateKeyManager(
            IList<string> callSequence,
            IEnumerable<CancellationToken> getCacheExpirationTokenReturnValues,
            IEnumerable<IReadOnlyCollection<IKey>> getAllKeysReturnValues,
            IEnumerable<Tuple<DateTimeOffset, DateTimeOffset, IKey>> createNewKeyCallbacks,
            IEnumerable<Tuple<DateTimeOffset, IEnumerable<IKey>, DefaultKeyResolution>> resolveDefaultKeyPolicyReturnValues,
            KeyManagementOptions keyManagementOptions = null)
        {
            var getCacheExpirationTokenReturnValuesEnumerator = getCacheExpirationTokenReturnValues.GetEnumerator();
            var mockKeyManager = new Mock<IKeyManager>(MockBehavior.Strict);
            mockKeyManager.Setup(o => o.GetCacheExpirationToken())
                .Returns(() =>
                {
                    callSequence.Add("GetCacheExpirationToken");
                    getCacheExpirationTokenReturnValuesEnumerator.MoveNext();
                    return getCacheExpirationTokenReturnValuesEnumerator.Current;
                });

            var getAllKeysReturnValuesEnumerator = getAllKeysReturnValues.GetEnumerator();
            mockKeyManager.Setup(o => o.GetAllKeys())
              .Returns(() =>
              {
                  callSequence.Add("GetAllKeys");
                  getAllKeysReturnValuesEnumerator.MoveNext();
                  return getAllKeysReturnValuesEnumerator.Current;
              });

            if (createNewKeyCallbacks != null)
            {
                var createNewKeyCallbacksEnumerator = createNewKeyCallbacks.GetEnumerator();
                mockKeyManager.Setup(o => o.CreateNewKey(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                    .Returns<DateTimeOffset, DateTimeOffset>((activationDate, expirationDate) =>
                    {
                        callSequence.Add("CreateNewKey");
                        createNewKeyCallbacksEnumerator.MoveNext();
                        Assert.Equal(createNewKeyCallbacksEnumerator.Current.Item1, activationDate);
                        Assert.Equal(createNewKeyCallbacksEnumerator.Current.Item2, expirationDate);
                        return createNewKeyCallbacksEnumerator.Current.Item3;
                    });
            }

            var resolveDefaultKeyPolicyReturnValuesEnumerator = resolveDefaultKeyPolicyReturnValues.GetEnumerator();
            var mockDefaultKeyResolver = new Mock<IDefaultKeyResolver>(MockBehavior.Strict);
            mockDefaultKeyResolver.Setup(o => o.ResolveDefaultKeyPolicy(It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<IKey>>()))
                .Returns<DateTimeOffset, IEnumerable<IKey>>((now, allKeys) =>
                {
                    callSequence.Add("ResolveDefaultKeyPolicy");
                    resolveDefaultKeyPolicyReturnValuesEnumerator.MoveNext();
                    Assert.Equal(resolveDefaultKeyPolicyReturnValuesEnumerator.Current.Item1, now);
                    Assert.Equal(resolveDefaultKeyPolicyReturnValuesEnumerator.Current.Item2, allKeys);
                    return resolveDefaultKeyPolicyReturnValuesEnumerator.Current.Item3;
                });

            return CreateKeyRingProvider(mockKeyManager.Object, mockDefaultKeyResolver.Object, keyManagementOptions);
        }

        private static KeyRingProvider CreateKeyRingProvider(ICacheableKeyRingProvider cacheableKeyRingProvider)
        {
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(m => m.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var options = new KeyManagementOptions();
            options.AuthenticatedEncryptorFactories.Add(mockEncryptorFactory.Object);

            return new KeyRingProvider(
                keyManager: null,
                keyManagementOptions: Options.Create(options),
                defaultKeyResolver: null,
                loggerFactory: NullLoggerFactory.Instance)
            {
                CacheableKeyRingProvider = cacheableKeyRingProvider
            };
        }

        private static ICacheableKeyRingProvider CreateKeyRingProvider(IKeyManager keyManager, IDefaultKeyResolver defaultKeyResolver, KeyManagementOptions keyManagementOptions= null)
        {
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(m => m.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            keyManagementOptions = keyManagementOptions ?? new KeyManagementOptions();
            keyManagementOptions.AuthenticatedEncryptorFactories.Add(mockEncryptorFactory.Object);

            return new KeyRingProvider(
                keyManager: keyManager,
                keyManagementOptions: Options.Create(keyManagementOptions),
                defaultKeyResolver: defaultKeyResolver,
                loggerFactory: NullLoggerFactory.Instance);
        }

        private static void AssertWithinJitterRange(DateTimeOffset actual, DateTimeOffset now)
        {
            // The jitter can cause the actual value to fall in the range [now + 80% of refresh period, now + 100% of refresh period)
            Assert.InRange(actual, now + TimeSpan.FromHours(24 * 0.8), now + TimeSpan.FromHours(24));
        }

        private static DateTime StringToDateTime(string input)
        {
            return DateTimeOffset.ParseExact(input, "u", CultureInfo.InvariantCulture).UtcDateTime;
        }

        private static IKey CreateKey()
        {
            var now = DateTimeOffset.Now;
            return CreateKey(
                string.Format(CultureInfo.InvariantCulture, "{0:u}", now),
                string.Format(CultureInfo.InvariantCulture, "{0:u}", now.AddDays(90)));
        }

        private static IKey CreateKey(string activationDate, string expirationDate, bool isRevoked = false)
        {
            var mockKey = new Mock<IKey>();
            mockKey.Setup(o => o.KeyId).Returns(Guid.NewGuid());
            mockKey.Setup(o => o.ActivationDate).Returns(DateTimeOffset.ParseExact(activationDate, "u", CultureInfo.InvariantCulture));
            mockKey.Setup(o => o.ExpirationDate).Returns(DateTimeOffset.ParseExact(expirationDate, "u", CultureInfo.InvariantCulture));
            mockKey.Setup(o => o.IsRevoked).Returns(isRevoked);
            mockKey.Setup(o => o.Descriptor).Returns(new Mock<IAuthenticatedEncryptorDescriptor>().Object);
            mockKey.Setup(o => o.CreateEncryptor()).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            return mockKey.Object;
        }
    }
}
