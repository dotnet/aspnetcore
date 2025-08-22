// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

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
                }),
                Tuple.Create(key1.ExpirationDate, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                {
                    DefaultKey = key2,
                    ShouldGenerateNewKey = false
                }),
            });

        // Act
        var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

        // Assert
        Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
        Assert.Equal(StringToDateTime("2016-03-01 00:00:00Z"), cacheableKeyRing.ExpirationTimeUtc);
        Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now));
        expirationCts.Cancel();
        Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now));
        Assert.Equal(new[] { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy", "ResolveDefaultKeyPolicy" }, callSequence);
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
    public void CreateCacheableKeyRing_GenerationRequired_NoDefaultKey_CreatesNewKeyWithImmediateActivation_NewKeyIsRevoked()
    {
        // Arrange
        var callSequence = new List<string>();

        var now = (DateTimeOffset)StringToDateTime("2015-03-01 00:00:00Z");
        var allKeys1 = Array.Empty<IKey>();

        // This could happen if there were a date-based revocation newer than 2015-03-01
        var newKey = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z", isRevoked: true);
        var allKeys2 = new[] { newKey };

        var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
            callSequence: callSequence,
            getCacheExpirationTokenReturnValues: new[] { CancellationToken.None, CancellationToken.None },
            getAllKeysReturnValues: new[] { allKeys1, allKeys2 },
            createNewKeyCallbacks: new[] {
                Tuple.Create(now, now + TimeSpan.FromDays(90), newKey)
            },
            resolveDefaultKeyPolicyReturnValues: new[]
            {
                Tuple.Create(now, (IEnumerable<IKey>)allKeys1, new DefaultKeyResolution()
                {
                    DefaultKey = null, // Since there are no keys
                    ShouldGenerateNewKey = true
                }),
                Tuple.Create(now, (IEnumerable<IKey>)allKeys2, new DefaultKeyResolution()
                {
                    DefaultKey = null, // Since all keys are revoked
                    ShouldGenerateNewKey = true
                })
            });

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => keyRingProvider.GetCacheableKeyRing(now)); // The would-be default key is revoked

        // Still make the usual calls - just throw before creating a keyring
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

    // The interesting time offsets are:
    //   0. now
    //   1. 24 hours from now, after a single refresh period
    //   2. 48 hours from now, after a single propagation cycle
    //   3. 72 hours from now, after a single refresh period and a single propagation cycle
    // Therefore, we test at:
    //   A. 12 hours from now, between (0) and (1)
    //   B. 36 hours from now, between (1) and (2)
    //   C. 60 hours from now, between (2) and (3)
    //   D. 84 hours from now, after (3)
    [Theory]
    [InlineData(12, 12, true, true)]
    [InlineData(12, 36, true, true)]
    [InlineData(12, 60, true, true)]
    [InlineData(12, 84, true, false)]
    [InlineData(36, 12, true, true)]
    [InlineData(36, 36, true, true)]
    [InlineData(36, 60, true, true)]
    [InlineData(36, 84, true, false)]
    [InlineData(60, 12, true, true)]
    [InlineData(60, 36, true, true)]
    [InlineData(60, 60, true, true)]
    [InlineData(60, 84, true, false)]
    [InlineData(84, 12, false, false)]
    [InlineData(84, 36, false, false)]
    [InlineData(84, 60, false, false)]
    [InlineData(84, 84, false, false)]
    public void CreateCacheableKeyRing_UnactivatedKeyAvailable(int hoursToExpiration1, int hoursToExpiration2, bool expectSecondResolution, bool expectGeneration)
    {
        // Arrange
        var actualCallSequence = new List<string>();

        DateTimeOffset now = StringToDateTime("2016-02-01 00:00:00Z");

        // Key1 is active, but Key2 is not
        DateTimeOffset activation1 = now - TimeSpan.FromHours(1);
        DateTimeOffset activation2 = now + TimeSpan.FromHours(1);

        DateTimeOffset expiration1 = now + TimeSpan.FromHours(hoursToExpiration1);
        DateTimeOffset expiration2 = now + TimeSpan.FromHours(hoursToExpiration2);

        // Some basic timeline constraints - if these fail, it's a test issue
        Assert.True(activation1 < now); // Key1 is active
        Assert.True(now < activation2); // Key2 is not yet active
        Assert.True(now < expiration1); // Key1 is not expired (also implies activation1 < expiration1)
        Assert.True(activation2 < expiration2); // Key2 is not well-formed (also implies Key2 is unexpired, now < expiration2)
        Assert.True(activation2 < expiration1); // Key1 and Key2 have overlapping activation periods - the alternative is covered in other tests
        // Specifically do not require that expiration1 < expiration2

        var key1 = CreateKey(activation1, expiration1);
        var key2 = CreateKey(activation2, expiration2);

        var generatedKey = CreateKey(expiration1, now + TimeSpan.FromDays(90));

        var key2ValidWhenKey1Expires = expiration1 < expiration2;

        var allKeys = new[] { key1, key2 };
        var allKeysAfterGeneration = new[] { key1, key2, generatedKey };

        var expectedCallSequence = new List<string> { "GetCacheExpirationToken", "GetAllKeys", "ResolveDefaultKeyPolicy" };

        var resolveDefaultKeyPolicyReturnValues = new List<Tuple<DateTimeOffset, IEnumerable<IKey>, DefaultKeyResolution>>()
        {
            Tuple.Create(now, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
            {
                DefaultKey = key1,
                ShouldGenerateNewKey = false // Let the key ring provider decide
            }),
        };

        if (expectSecondResolution)
        {
            expectedCallSequence.Add("ResolveDefaultKeyPolicy");

            resolveDefaultKeyPolicyReturnValues.Add(
                Tuple.Create(expiration1, (IEnumerable<IKey>)allKeys, new DefaultKeyResolution()
                {
                    DefaultKey = key2ValidWhenKey1Expires ? key2 : null,
                    FallbackKey = key2ValidWhenKey1Expires ? null : key2,
                    ShouldGenerateNewKey = !key2ValidWhenKey1Expires
                }));
        }

        if (expectGeneration)
        {
            expectedCallSequence.Add("CreateNewKey");
            // Repeat the initial calls, but not the second resolution
            for (int i = 0; i < 3; i++)
            {
                expectedCallSequence.Add(expectedCallSequence[i]);
            }

            resolveDefaultKeyPolicyReturnValues.Add(
                Tuple.Create(now, (IEnumerable<IKey>)allKeysAfterGeneration, new DefaultKeyResolution()
                {
                    DefaultKey = key1,
                    ShouldGenerateNewKey = false // Let the key ring provider decide
                }));

            // We don't repeat the second resolution because the key ring provider should not need to resolve again
        }

        var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
            callSequence: actualCallSequence,
            getCacheExpirationTokenReturnValues: new[] { CancellationToken.None, CancellationToken.None },
            getAllKeysReturnValues: new[] { allKeys, allKeysAfterGeneration },
            createNewKeyCallbacks: new[] {
                Tuple.Create(expiration1, now + TimeSpan.FromDays(90), CreateKey())
            },
            resolveDefaultKeyPolicyReturnValues: resolveDefaultKeyPolicyReturnValues);

        // Act
        var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

        // Assert
        Assert.Equal(key1.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
        Assert.Equal(expectedCallSequence, actualCallSequence);
    }

    [Fact]
    public void CreateCacheableKeyRing_ForceKeyGeneration()
    {
        // Arrange
        var actualCallSequence = new List<string>();

        DateTimeOffset now = StringToDateTime("2016-02-01 00:00:00Z");

        // Key is activate and not close to expiration
        DateTimeOffset activation = now - TimeSpan.FromDays(30);
        DateTimeOffset expiration = now + TimeSpan.FromDays(30);

        var key = CreateKey(activation, expiration);
        var generatedKey = CreateKey(expiration, now + TimeSpan.FromDays(90));

        var allKeysBefore = new[] { key };
        var allKeysAfter = new[] { key, generatedKey };

        var keyRingProvider = SetupCreateCacheableKeyRingTestAndCreateKeyManager(
            callSequence: actualCallSequence,
            getCacheExpirationTokenReturnValues: new[] { CancellationToken.None, CancellationToken.None },
            getAllKeysReturnValues: new[] { allKeysBefore, allKeysAfter },
            createNewKeyCallbacks: new[] {
                Tuple.Create(expiration, now + TimeSpan.FromDays(90), generatedKey)
            },
            resolveDefaultKeyPolicyReturnValues: new[] {
                Tuple.Create(now, (IEnumerable<IKey>)allKeysBefore, new DefaultKeyResolution()
                {
                    DefaultKey = key,
                    ShouldGenerateNewKey = true, // Force re-generation
                }),
                Tuple.Create(now, (IEnumerable<IKey>)allKeysAfter, new DefaultKeyResolution()
                {
                    DefaultKey = key,
                    ShouldGenerateNewKey = true, // Force re-generation
                }),
            });

        // Act
        var cacheableKeyRing = keyRingProvider.GetCacheableKeyRing(now);

        // Assert
        Assert.Equal(key.KeyId, cacheableKeyRing.KeyRing.DefaultKeyId);
        string[] expectedCallSequence =
        [
            "GetCacheExpirationToken",
            "GetAllKeys",
            "ResolveDefaultKeyPolicy",
            "CreateNewKey",
            "GetCacheExpirationToken",
            "GetAllKeys",
            "ResolveDefaultKeyPolicy",
        ];
        Assert.Equal(expectedCallSequence, actualCallSequence);
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
        var retVal2 = keyRingProvider.GetCurrentKeyRingCore(now + TimeSpan.FromHours(1), forceRefresh: true);

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
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        backgroundGetKeyRingTask.Wait(testTimeout);
        var backgroundRetVal = backgroundGetKeyRingTask.GetAwaiter().GetResult();
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        // Assert - underlying provider only should have been called once
        Assert.Same(expectedKeyRing, foregroundRetVal);
        Assert.Same(expectedKeyRing, backgroundRetVal);
        mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(It.IsAny<DateTimeOffset>()), Times.Once);
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
        ExceptionAssert.Throws<Exception>(() => keyRingProvider.GetCurrentKeyRingCore(throwKeyRingTime, forceRefresh: true), "How exceptional."); // forceRefresh to wait for exception
        Assert.Same(originalKeyRing, keyRingProvider.GetCurrentKeyRingCore(throwKeyRingTime)); // Seeing the exception didn't clobber the cache
        Assert.Same(updatedKeyRing, keyRingProvider.GetCurrentKeyRingCore(updatedKeyRingTime, forceRefresh: true)); // forceRefresh to wait for updated value
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
                Assert.True(resolveDefaultKeyPolicyReturnValuesEnumerator.MoveNext());
                var current = resolveDefaultKeyPolicyReturnValuesEnumerator.Current;
                Assert.Equal(current.Item1, now);
                Assert.Equal(current.Item2, allKeys);
                return current.Item3;
            });

        return CreateKeyRingProvider(mockKeyManager.Object, mockDefaultKeyResolver.Object, keyManagementOptions);
    }

    [Fact]
    public async Task MultipleThreadsSeeExpiredCachedValue()
    {
        const int taskCount = 10;
        var time1 = StringToDateTime("2015-03-01 00:00:00Z");
        var time2 = time1.AddHours(1);

        var expectedKeyRing1 = new Mock<IKeyRing>().Object;
        var expectedKeyRing2 = new Mock<IKeyRing>().Object;

        var cts = new CancellationTokenSource();

        var mockCacheableKeyRingProvider = new Mock<ICacheableKeyRingProvider>();
        mockCacheableKeyRingProvider
            .Setup(o => o.GetCacheableKeyRing(time1))
            .Returns(new CacheableKeyRing(
                expirationToken: cts.Token,
                expirationTime: time1.AddDays(1),
                keyRing: expectedKeyRing1));
        mockCacheableKeyRingProvider
            .Setup(o => o.GetCacheableKeyRing(time2))
            .Returns(new CacheableKeyRing(
                expirationToken: CancellationToken.None,
                expirationTime: time2.AddDays(1),
                keyRing: expectedKeyRing2));

        var keyRingProvider = CreateKeyRingProvider(mockCacheableKeyRingProvider.Object);

        Assert.Same(expectedKeyRing1, keyRingProvider.GetCurrentKeyRingCore(time1)); // Ensure the cache is populated

        cts.Cancel(); // Invalidate (but don't clear) the cached value

        var tasks = new Task<IKeyRing>[taskCount];
        for (var i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var keyRing = keyRingProvider.GetCurrentKeyRingCore(time2);
                return keyRing;
            });
        }

        var actualKeyRings = await Task.WhenAll(tasks);
        Assert.All(actualKeyRings, actualKeyRing => ReferenceEquals(expectedKeyRing1, actualKeyRing));

        mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(time1), Times.Once);

        // We'd like there to be exactly one call, but it's possible that the first thread will actually
        // release the critical section before the last thread attempts to acquire it and the work will
        // be redone (by design, since the refresh is forced).
        // Even asserting < taskCount is probabilistic - it's possible, though very unlikely, that each
        // thread could finish before the next even attempts to enter the criticial section.  If this
        // proves to be flaky, we could increase taskCount or intentionally slow down GetCacheableKeyRing.
        mockCacheableKeyRingProvider.Verify(o => o.GetCacheableKeyRing(time2), Times.AtMost(taskCount - 1));

        // Verify that the updated value eventually becomes available (5 seconds max)
        for (var i = 0; i < 10; i++)
        {
            var updatedKeyRing = keyRingProvider.GetCurrentKeyRingCore(time2);
            if (ReferenceEquals(expectedKeyRing2, updatedKeyRing))
            {
                break;
            }

            Assert.Same(expectedKeyRing1, updatedKeyRing);
            await Task.Delay(500);
        }
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

    private static ICacheableKeyRingProvider CreateKeyRingProvider(IKeyManager keyManager, IDefaultKeyResolver defaultKeyResolver, KeyManagementOptions keyManagementOptions = null)
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
        return CreateKey(
            DateTimeOffset.ParseExact(activationDate, "u", CultureInfo.InvariantCulture),
            DateTimeOffset.ParseExact(expirationDate, "u", CultureInfo.InvariantCulture),
            isRevoked);
    }

    private static IKey CreateKey(DateTimeOffset activationDate, DateTimeOffset expirationDate, bool isRevoked = false)
    {
        var mockKey = new Mock<IKey>();
        mockKey.Setup(o => o.KeyId).Returns(Guid.NewGuid());
        mockKey.Setup(o => o.ActivationDate).Returns(activationDate);
        mockKey.Setup(o => o.ExpirationDate).Returns(expirationDate);
        mockKey.Setup(o => o.IsRevoked).Returns(isRevoked);
        mockKey.Setup(o => o.Descriptor).Returns(new Mock<IAuthenticatedEncryptorDescriptor>().Object);
        mockKey.Setup(o => o.CreateEncryptor()).Returns(new Mock<IAuthenticatedEncryptor>().Object);
        return mockKey.Object;
    }
}
