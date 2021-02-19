// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class PrerenderComponentApplicationStoreTest
    {
        [Fact]
        public async Task PersistStateAsync_PersistsGivenState()
        {
            // Arrange
            var expected = "eyJNeVZhbHVlIjoiQVFJREJBPT0ifQ==";
            var store = new PrerenderComponentApplicationStore();
            var state = new Dictionary<string, byte[]>()
            {
                ["MyValue"] = new byte[] {1,2,3,4}
            };

            // Act
            await store.PersistStateAsync(state);

            // Assert
            Assert.Equal(expected, store.PersistedState);
        }

        [Fact]
        public async Task GetPersistedStateAsync_RestoresPreexistingStateAsync()
        {
            // Arrange
            var persistedState = "eyJNeVZhbHVlIjoiQVFJREJBPT0ifQ==";
            var store = new PrerenderComponentApplicationStore(persistedState);
            var expected = new Dictionary<string, byte[]>()
            {
                ["MyValue"] = new byte[] { 1, 2, 3, 4 }
            };

            // Act
            var state = await store.GetPersistedStateAsync();

            // Assert
            Assert.Equal(expected, state);
        }
    }
}
