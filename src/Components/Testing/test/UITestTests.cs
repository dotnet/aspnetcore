// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class UITestTests
{
    [Fact]
    public async Task DisposeAsync_DisposesRegisteredResourcesInReverseOrder()
    {
        var disposalOrder = new List<int>();
        var test = new TestUITest();
        test.Register(new TestAsyncDisposable(() => disposalOrder.Add(1)));
        test.Register(new TestAsyncDisposable(() => disposalOrder.Add(2)));

        await ((IAsyncDisposable)test).DisposeAsync();

        Assert.Equal([2, 1], disposalOrder);
    }

    [Fact]
    public async Task DisposeAsync_AttemptsAllRegisteredResourcesWhenDisposalFails()
    {
        var disposalOrder = new List<int>();
        var test = new TestUITest();
        test.Register(new TestAsyncDisposable(() => disposalOrder.Add(1)));
        test.Register(new TestAsyncDisposable(() => throw new InvalidOperationException("Failed to dispose.")));
        test.Register(new TestAsyncDisposable(() => disposalOrder.Add(3)));

        var exception = await Assert.ThrowsAsync<AggregateException>(
            async () => await ((IAsyncDisposable)test).DisposeAsync());

        Assert.Equal([3, 1], disposalOrder);
        Assert.IsType<InvalidOperationException>(Assert.Single(exception.InnerExceptions));
    }

    private sealed class TestUITest : UITest, ITestArtifactManager
    {
        public T Register<T>(T disposable) where T : IAsyncDisposable
            => RegisterForDisposal(disposable);

        bool ITestArtifactManager.ShouldSaveArtifacts() => false;

        string ITestArtifactManager.CreateArtifactDirectory(string category)
            => Path.Combine(Path.GetTempPath(), category, Guid.NewGuid().ToString("N"));

        void ITestArtifactManager.AddArtifacts(IReadOnlyList<string> paths)
        {
        }
    }

    private sealed class TestAsyncDisposable(Action dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            dispose();
            return ValueTask.CompletedTask;
        }
    }
}
