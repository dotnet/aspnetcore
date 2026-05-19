// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class ProtectedPrerenderComponentApplicationStateTest
{
    private static readonly IDataProtectionProvider _provider = new EphemeralDataProtectionProvider();
    private static readonly IDataProtector _protector = _provider.CreateProtector("Microsoft.AspNetCore.Components.Server.State");

    [Fact]
    public async Task PersistStateAsync_ProtectsPersistedState()
    {
        // Arrange
        var expected = @"{""MyValue"":""AQIDBA==""}";
        var store = new ProtectedPrerenderComponentApplicationStore(_provider);

        var state = new Dictionary<string, byte[]>()
        {
            ["MyValue"] = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        await store.PersistStateAsync(state);

        // Assert
        Assert.Equal(expected, _protector.Unprotect(store.PersistedState));
    }

    [Fact]
    public async Task GetPersistStateAsync_CanUnprotectPersistedState()
    {
        // Arrange
        var expectedState = new Dictionary<string, byte[]>()
        {
            ["MyValue"] = new byte[] { 1, 2, 3, 4 }
        };

        var persistedState = Convert.ToBase64String(_protector.Protect(JsonSerializer.SerializeToUtf8Bytes(expectedState)));
        var store = new ProtectedPrerenderComponentApplicationStore(persistedState, _provider);

        // Act
        var restored = await store.GetPersistedStateAsync();

        // Assert
        Assert.Equal(
            expectedState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
            restored.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));
    }

    [Fact]
    public void Constructor_ThrowsWhenItCanNotUnprotectThePayload()
    {
        // Arrange
        var expectedState = new Dictionary<string, byte[]>()
        {
            ["MyValue"] = new byte[] { 1, 2, 3, 4 }
        };

        var persistedState = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(expectedState));

        // Act & Assert
        Assert.Throws<CryptographicException>(() =>
            new ProtectedPrerenderComponentApplicationStore(persistedState, _provider));
    }
}
