// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
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
            Assert.Equal(expectedState, restored);
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
}
