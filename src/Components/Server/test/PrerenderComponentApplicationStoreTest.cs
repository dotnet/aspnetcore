// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Server.Tests;

public class PrerenderComponentApplicationStoreTest
{
    [Fact]
    public async Task PersistStateAsync_SerializesAndRestoresState()
    {
        var store = new PrerenderComponentApplicationStore();
        var state = new Dictionary<string, byte[]>
        {
            ["first"] = [1, 2, 3],
            ["second"] = Encoding.UTF8.GetBytes("value"),
        };

        await store.PersistStateAsync(state);

        var persistedState = Assert.IsType<string>(store.PersistedState);
        var restoredStore = new PrerenderComponentApplicationStore(persistedState);
        var restoredState = await restoredStore.GetPersistedStateAsync();

        Assert.Equal(state, restoredState);
    }
}
