// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

public class CacheableKeyRingTests
{
    [Fact]
    public void IsValid_NullKeyRing_ReturnsFalse()
    {
        Assert.False(CacheableKeyRing.IsValid(null, DateTime.UtcNow));
    }

    [Fact]
    public void IsValid_CancellationTokenTriggered_ReturnsFalse()
    {
        // Arrange
        var keyRing = new Mock<IKeyRing>().Object;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var cts = new CancellationTokenSource();
        var cacheableKeyRing = new CacheableKeyRing(cts.Token, now.AddHours(1), keyRing);

        // Act & assert
        Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now.UtcDateTime));
        cts.Cancel();
        Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now.UtcDateTime));
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        // Arrange
        var keyRing = new Mock<IKeyRing>().Object;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var cts = new CancellationTokenSource();
        var cacheableKeyRing = new CacheableKeyRing(cts.Token, now.AddHours(1), keyRing);

        // Act & assert
        Assert.True(CacheableKeyRing.IsValid(cacheableKeyRing, now.UtcDateTime));
        Assert.False(CacheableKeyRing.IsValid(cacheableKeyRing, now.AddHours(1).UtcDateTime));
    }

    [Fact]
    public void KeyRing_Prop()
    {
        // Arrange
        var keyRing = new Mock<IKeyRing>().Object;
        var cacheableKeyRing = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.Now, keyRing);

        // Act & assert
        Assert.Same(keyRing, cacheableKeyRing.KeyRing);
    }
}
