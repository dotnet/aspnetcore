// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

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
            ["MyValue"] = new byte[] { 1, 2, 3, 4 }
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
        var expected = new Dictionary<string, ReadOnlySequence<byte>>()
        {
            ["MyValue"] = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4 })
        };

        // Act
        var state = await store.GetPersistedStateAsync();

        // Assert
        Assert.Equal(
            expected.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
            state.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));
    }
}
