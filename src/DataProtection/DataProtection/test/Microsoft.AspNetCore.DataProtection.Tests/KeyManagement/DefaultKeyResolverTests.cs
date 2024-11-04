// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

public class DefaultKeyResolverTests
{
    [Fact]
    public void ResolveDefaultKeyPolicy_EmptyKeyRing_ReturnsNullDefaultKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy(DateTimeOffset.Now, new IKey[0]);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_ValidExistingKey_ReturnsExistingKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
        var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2016-02-20 23:59:00Z", key1, key2);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_ValidExistingKey_AllowsForClockSkew_KeysStraddleSkewLine_ReturnsExistingKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
        var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2016-02-29 23:59:00Z", key1, key2);

        // Assert
        Assert.Same(key2, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_ValidExistingKey_AllowsForClockSkew_AllKeysInFuture_ReturnsExistingKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2016-02-29 23:59:00Z", key1);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_ValidExistingKey_NoSuccessor_ReturnsExistingKey_DoesNotSignalGenerateNewKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2016-02-29 23:59:00Z", key1);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey); // Does not reflect pending expiration
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_ValidExistingKey_NoLegitimateSuccessor_ReturnsExistingKey_DoesNotSignalGenerateNewKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
        var key2 = CreateKey("2016-03-01 00:00:00Z", "2017-03-01 00:00:00Z", isRevoked: true);
        var key3 = CreateKey("2016-03-01 00:00:00Z", "2016-03-02 00:00:00Z"); // key expires too soon

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2016-02-29 23:50:00Z", key1, key2, key3);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey); // Does not reflect pending expiration
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_MostRecentKeyIsInvalid_BecauseOfRevocation_ReturnsNull()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
        var key2 = CreateKey("2015-03-02 00:00:00Z", "2016-03-01 00:00:00Z", isRevoked: true);

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2015-04-01 00:00:00Z", key1, key2);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_MostRecentKeyIsInvalid_BecauseOfFailureToDecipher_ReturnsNull()
    {
        // Arrange
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");
        var key2 = CreateKey("2015-03-02 00:00:00Z", "2016-03-01 00:00:00Z", createEncryptorThrows: true);
        var resolver = CreateDefaultKeyResolver();

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2015-04-01 00:00:00Z", key1, key2);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_FutureKeyIsValidAndWithinClockSkew_ReturnsFutureKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2015-02-28 23:55:00Z", key1);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_FutureKeyIsValidButNotWithinClockSkew_ReturnsNull()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2016-03-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2015-02-28 23:00:00Z", key1);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_IgnoresExpiredOrRevokedFutureKeys()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2015-03-01 00:00:00Z", "2014-03-01 00:00:00Z"); // expiration before activation should never occur
        var key2 = CreateKey("2015-03-01 00:01:00Z", "2015-04-01 00:00:00Z", isRevoked: true);
        var key3 = CreateKey("2015-03-01 00:02:00Z", "2015-04-01 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2015-02-28 23:59:00Z", key1, key2, key3);

        // Assert
        Assert.Same(key3, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_FallbackKey_SelectsLatestBeforePriorPropagationWindow_IgnoresRevokedKeys()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-01 00:00:00Z");
        var key2 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-02 00:00:00Z");
        var key3 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-03 00:00:00Z", isRevoked: true);
        var key4 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-04 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2000-01-05 00:00:00Z", key1, key2, key3, key4);

        // Assert
        Assert.Same(key2, resolution.FallbackKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_FallbackKey_SelectsLatestBeforePriorPropagationWindow_IgnoresFailures()
    {
        // Arrange
        var key1 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-01 00:00:00Z");
        var key2 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-02 00:00:00Z");
        var key3 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-03 00:00:00Z", createEncryptorThrows: true);
        var key4 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-04 00:00:00Z");
        var resolver = CreateDefaultKeyResolver();

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2000-01-05 00:00:00Z", key1, key2, key3, key4);

        // Assert
        Assert.Same(key2, resolution.FallbackKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_FallbackKey_NoNonRevokedKeysBeforePriorPropagationWindow_SelectsEarliestNonRevokedKey()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();
        var key1 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-03 00:00:00Z", isRevoked: true);
        var key2 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-04 00:00:00Z");
        var key3 = CreateKey("2010-01-01 00:00:00Z", "2010-01-01 00:00:00Z", creationDate: "2000-01-05 00:00:00Z");

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy("2000-01-05 00:00:00Z", key1, key2, key3);

        // Assert
        Assert.Same(key2, resolution.FallbackKey);
        Assert.True(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void ResolveDefaultKeyPolicy_OlderBadKeyVersusNewerGoodKey()
    {
        // In https://github.com/dotnet/aspnetcore/issues/57137, we encountered an issue
        // where we selected the oldest unpropagated key, even though it could not be decrypted.
        // Choosing the oldest unpropagated key makes sense in principle, but we were late
        // enough in the release cycle that it made more sense to revert to the older behavior
        // (preferring the most recently activated key, regardless of propagation) than to
        // attempt a forward fix (i.e. harden the fancier logic against decryption failures).
        // This test is intended to ensure that the bad key is never chosen, regardless of
        // how we order the activated keys in the future.

        // Arrange
        var resolver = CreateDefaultKeyResolver();

        var now = ParseDateTimeOffset("2010-01-01 00:00:00Z");

        var creation1 = now - TimeSpan.FromHours(1);
        var creation2 = creation1 - TimeSpan.FromHours(1);
        var activation1 = creation1;
        var activation2 = creation2;
        var expiration1 = creation1 + TimeSpan.FromDays(90);
        var expiration2 = creation2 + TimeSpan.FromDays(90);

        // Both active (key 1 more recently), neither propagated, key2 can't be decrypted
        var key1 = CreateKey(activation1, expiration1, creationDate: creation1);
        var key2 = CreateKey(activation2, expiration2, creationDate: creation2, createEncryptorThrows: true);

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy(now, [key1, key2]);

        // Assert
        Assert.Same(key1, resolution.DefaultKey);
        Assert.False(resolution.ShouldGenerateNewKey);
    }

    [Fact]
    public void CreateEncryptor_NoRetryOnNullReturn()
    {
        // Arrange
        var resolver = CreateDefaultKeyResolver();

        var now = ParseDateTimeOffset("2010-01-01 00:00:00Z");

        int descriptorFactoryCalls = 0;

        // Retries don't work with mock keys
        var key = new Key(
            Guid.NewGuid(),
            creationDate: now.AddDays(-3), // Propagated
            activationDate: now.AddDays(-1), // Activated
            expirationDate: now.AddDays(14), // Unexpired
            encryptorFactories: [], // Causes CreateEncryptor to return null
            descriptorFactory: () =>
            {
                descriptorFactoryCalls++;
                throw new InvalidOperationException("Shouldn't be called");
            });

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy(now, [key]);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.Null(resolution.FallbackKey);
        Assert.True(resolution.ShouldGenerateNewKey);

        Assert.Equal(0, descriptorFactoryCalls); // Not retried
    }

    [Theory]
    [InlineData(0)] // Retries disabled (as by appcontext switch)
    [InlineData(1)]
    [InlineData(10)]
    public void CreateEncryptor_FirstAttemptIsNotARetry(int maxRetries)
    {
        // Arrange
        var options = Options.Create(new KeyManagementOptions()
        {
            MaximumTotalDefaultKeyResolverRetries = maxRetries,
            DefaultKeyResolverRetryDelay = TimeSpan.Zero,
        });

        var resolver = new DefaultKeyResolver(options, NullLoggerFactory.Instance);

        var now = ParseDateTimeOffset("2010-01-01 00:00:00Z");

        var keyId1 = Guid.NewGuid();
        var creation1 = now.AddDays(-3);
        var activation1 = creation1.AddDays(2);
        var expiration1 = creation1.AddDays(90);

        // Newer but still propagated => preferred
        var keyId2 = Guid.NewGuid();
        var creation2 = creation1.AddHours(1);
        var activation2 = activation1.AddHours(1);
        var expiration2 = expiration1.AddHours(1);

        var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
        var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();

        var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
        mockEncryptorFactory
            .Setup(o => o.CreateEncryptorInstance(It.IsAny<Key>()))
            .Returns<Key>(key =>
            {
                _ = key.Descriptor; // A normal implementation would call this
                return mockEncryptor.Object;
            });

        var descriptorFactoryCalls1 = 0;
        var descriptorFactoryCalls2 = 0;

        // Retries don't work with mock keys
        var key1 = new Key(
            keyId1,
            creation1,
            activation1,
            expiration1,
            encryptorFactories: [mockEncryptorFactory.Object],
            descriptorFactory: () =>
            {
                descriptorFactoryCalls1++;
                return mockDescriptor.Object;
            });

        var key2 = new Key(
            keyId2,
            creation2,
            activation2,
            expiration2,
            encryptorFactories: [mockEncryptorFactory.Object],
            descriptorFactory: () =>
            {
                descriptorFactoryCalls2++;
                throw new InvalidOperationException("Simulated decryption failure");
            });

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy(now, [key1, key2]);

        // Assert
        Assert.Null(resolution.DefaultKey);
        Assert.Same(key1, resolution.FallbackKey);
        Assert.True(resolution.ShouldGenerateNewKey);

        Assert.Equal(1, descriptorFactoryCalls1); // 1 try
        Assert.Equal(1 + maxRetries, descriptorFactoryCalls2); // 1 try plus max retries
    }

    [Fact]
    public void CreateEncryptor_SucceedsOnRetry()
    {
        // Arrange
        var options = Options.Create(new KeyManagementOptions()
        {
            MaximumTotalDefaultKeyResolverRetries = 3,
            DefaultKeyResolverRetryDelay = TimeSpan.Zero,
        });

        var resolver = new DefaultKeyResolver(options, NullLoggerFactory.Instance);

        var now = ParseDateTimeOffset("2010-01-01 00:00:00Z");

        var creation = now.AddDays(-3);
        var activation = creation.AddDays(2);
        var expiration = creation.AddDays(90);

        var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
        var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();

        var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
        mockEncryptorFactory
            .Setup(o => o.CreateEncryptorInstance(It.IsAny<Key>()))
            .Returns<Key>(key =>
            {
                _ = key.Descriptor; // A normal implementation would call this
                return mockEncryptor.Object;
            });

        var descriptorFactoryCalls = 0;

        // Retries don't work with mock keys
        var key = new Key(
            Guid.NewGuid(),
            creation,
            activation,
            expiration,
            encryptorFactories: [mockEncryptorFactory.Object],
            descriptorFactory: () =>
            {
                descriptorFactoryCalls++;
                if (descriptorFactoryCalls == 1)
                {
                    throw new InvalidOperationException("Simulated decryption failure");
                }
                return mockDescriptor.Object;
            });

        // Act
        var resolution = resolver.ResolveDefaultKeyPolicy(now, [key]);

        // Assert
        Assert.Same(key, resolution.DefaultKey);
        Assert.Equal(2, descriptorFactoryCalls); // 1 try plus 1 retry
    }

    private static IDefaultKeyResolver CreateDefaultKeyResolver()
    {
        return new DefaultKeyResolver(Options.Create(new KeyManagementOptions()), NullLoggerFactory.Instance);
    }

    private static IKey CreateKey(string activationDate, string expirationDate, string creationDate = null, bool isRevoked = false, bool createEncryptorThrows = false)
    {
        return CreateKey(ParseDateTimeOffset(activationDate), ParseDateTimeOffset(expirationDate), creationDate == null ? (DateTimeOffset?)null : ParseDateTimeOffset(creationDate), isRevoked, createEncryptorThrows);
    }

    private static IKey CreateKey(DateTimeOffset activationDate, DateTimeOffset expirationDate, DateTimeOffset? creationDate = null, bool isRevoked = false, bool createEncryptorThrows = false)
    {
        var mockKey = new Mock<IKey>();
        mockKey.Setup(o => o.KeyId).Returns(Guid.NewGuid());
        mockKey.Setup(o => o.CreationDate).Returns(creationDate ?? DateTimeOffset.MinValue);
        mockKey.Setup(o => o.ActivationDate).Returns(activationDate);
        mockKey.Setup(o => o.ExpirationDate).Returns(expirationDate);
        mockKey.Setup(o => o.IsRevoked).Returns(isRevoked);
        if (createEncryptorThrows)
        {
            mockKey.Setup(o => o.CreateEncryptor()).Throws(new Exception("This method fails."));
        }
        else
        {
            mockKey.Setup(o => o.CreateEncryptor()).Returns(Mock.Of<IAuthenticatedEncryptor>());
        }

        return mockKey.Object;
    }

    private static DateTimeOffset ParseDateTimeOffset(string dto)
    {
        return DateTimeOffset.ParseExact(dto, "u", CultureInfo.InvariantCulture);
    }
}

internal static class DefaultKeyResolverExtensions
{
    public static DefaultKeyResolution ResolveDefaultKeyPolicy(this IDefaultKeyResolver resolver, string now, params IKey[] allKeys)
    {
        return resolver.ResolveDefaultKeyPolicy(DateTimeOffset.ParseExact(now, "u", CultureInfo.InvariantCulture), (IEnumerable<IKey>)allKeys);
    }
}
