// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;

public class ServerAuthenticationStateProviderTest
{
    [Fact]
    public async Task CannotProvideAuthenticationStateBeforeInitialization()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ServerAuthenticationStateProvider()
                .GetAuthenticationStateAsync());
    }

    [Fact]
    public async Task SuppliesAuthenticationStateWithFixedUser()
    {
        // Arrange
        var user = new ClaimsPrincipal();
        var provider = new ServerAuthenticationStateProvider();

        // Act 1
        var expectedAuthenticationState1 = new AuthenticationState(user);
        provider.SetAuthenticationState(Task.FromResult(expectedAuthenticationState1));

        // Assert 1
        var actualAuthenticationState1 = await provider.GetAuthenticationStateAsync();
        Assert.NotNull(actualAuthenticationState1);
        Assert.Same(expectedAuthenticationState1, actualAuthenticationState1);

        // Act 2: Show we can update it further
        var expectedAuthenticationState2 = new AuthenticationState(user);
        provider.SetAuthenticationState(Task.FromResult(expectedAuthenticationState2));

        // Assert 2
        var actualAuthenticationState2 = await provider.GetAuthenticationStateAsync();
        Assert.NotNull(actualAuthenticationState2);
        Assert.NotSame(actualAuthenticationState1, actualAuthenticationState2);
        Assert.Same(expectedAuthenticationState2, actualAuthenticationState2);
    }
}
