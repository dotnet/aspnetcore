// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class ContentBlockTests
{
    private class TestBlock : ContentBlock
    {
        public void TriggerChanged() => NotifyChanged();
    }

    [Fact]
    public void OnChanged_CallbackFiresOnNotifyChanged()
    {
        var block = new TestBlock();
        var fired = false;
        block.OnChanged(() => fired = true);

        block.TriggerChanged();

        Assert.True(fired);
    }

    [Fact]
    public void OnChanged_MultipleCallbacksAllFire()
    {
        var block = new TestBlock();
        var count = 0;
        block.OnChanged(() => count++);
        block.OnChanged(() => count++);

        block.TriggerChanged();

        Assert.Equal(2, count);
    }

    [Fact]
    public void OnChanged_DisposingStopsCallback()
    {
        var block = new TestBlock();
        var count = 0;
        var reg = block.OnChanged(() => count++);

        block.TriggerChanged();
        Assert.Equal(1, count);

        reg.Dispose();
        block.TriggerChanged();
        Assert.Equal(1, count);
    }

    [Fact]
    public void OnChanged_DoubleDisposeDoesNotThrow()
    {
        var block = new TestBlock();
        var reg = block.OnChanged(() => { });

        reg.Dispose();
        reg.Dispose();
    }

    [Fact]
    public void OnChanged_DisposingInsideCallbackDoesNotThrow()
    {
        var block = new TestBlock();
        ContentBlockChangedSubscription reg = default;
        reg = block.OnChanged(() => reg.Dispose());

        block.TriggerChanged();
    }

    [Fact]
    public void OnChanged_ReturnsConcreteStructType()
    {
        var block = new TestBlock();
        ContentBlockChangedSubscription reg = block.OnChanged(() => { });

        Assert.IsType<ContentBlockChangedSubscription>(reg);
        reg.Dispose();
    }
}
