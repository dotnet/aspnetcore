// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

public class IHostInitializerTest
{
    [Fact]
    public async Task DefaultsAndCancellationTokenArePassedThrough()
    {
        var initializer = new TestHostInitializer();
        using var cancellationTokenSource = new CancellationTokenSource();

        await initializer.InitializeAsync(cancellationTokenSource.Token);

        Assert.Equal(0, ((IHostInitializer)initializer).Order);
        Assert.False(((IHostInitializer)initializer).RequiresJSInterop);
        Assert.Equal(cancellationTokenSource.Token, initializer.CancellationToken);
    }

    private sealed class TestHostInitializer : IHostInitializer
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
