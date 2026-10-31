// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class RestoreContextTest
{
    [Theory]
    [InlineData(RestoreBehavior.Default)]
    [InlineData(RestoreBehavior.SkipLastSnapshot)]
    public void ShouldRestore_InitialValueContext_WithDefaultOrSkipLastSnapshot(RestoreBehavior behavior)
    {
        var options = new RestoreOptions { RestoreBehavior = behavior };

        var result = RestoreContext.InitialValue.ShouldRestore(options);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRestore_InitialValueContext_WithSkipInitialValue()
    {
        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.SkipInitialValue };

        var result = RestoreContext.InitialValue.ShouldRestore(options);

        Assert.False(result);
    }

    [Theory]
    [InlineData(RestoreBehavior.Default, true, true)]
    [InlineData(RestoreBehavior.Default, false, true)]
    [InlineData(RestoreBehavior.SkipInitialValue, true, false)]
    [InlineData(RestoreBehavior.SkipInitialValue, false, false)]
    public void ShouldRestore_InitialValueContext_ShouldRestore_IsIndependentOfAllowUpdates(RestoreBehavior behavior, bool allowUpdates, bool expectedResult)
    {
        var options = new RestoreOptions { RestoreBehavior = behavior, AllowUpdates = allowUpdates };

        var result = RestoreContext.InitialValue.ShouldRestore(options);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(RestoreBehavior.Default)]
    [InlineData(RestoreBehavior.SkipInitialValue)]
    public void ShouldRestore_LastSnapshotContext_WithDefaultOrSkipInitialValue(RestoreBehavior behavior)
    {
        var options = new RestoreOptions { RestoreBehavior = behavior };

        var result = RestoreContext.LastSnapshot.ShouldRestore(options);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRestore_LastSnapshotContext_WithSkipLastSnapshot()
    {
        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.SkipLastSnapshot };

        var result = RestoreContext.LastSnapshot.ShouldRestore(options);

        Assert.False(result);
    }

    [Theory]
    [InlineData(RestoreBehavior.Default, true, true)]
    [InlineData(RestoreBehavior.Default, false, true)]
    [InlineData(RestoreBehavior.SkipLastSnapshot, true, false)]
    [InlineData(RestoreBehavior.SkipLastSnapshot, false, false)]
    public void ShouldRestore_LastSnapshotContext_ShouldRestore_IsIndependentOfAllowUpdates(RestoreBehavior behavior, bool allowUpdates, bool expectedResult)
    {
        var options = new RestoreOptions { RestoreBehavior = behavior, AllowUpdates = allowUpdates };

        var result = RestoreContext.LastSnapshot.ShouldRestore(options);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(RestoreBehavior.Default)]
    [InlineData(RestoreBehavior.SkipInitialValue)]
    [InlineData(RestoreBehavior.SkipLastSnapshot)]
    public void ShouldRestore_ValueUpdateContext_WithoutAllowUpdates(RestoreBehavior behavior)
    {
        var options = new RestoreOptions { RestoreBehavior = behavior };

        var result = RestoreContext.ValueUpdate.ShouldRestore(options);

        Assert.False(result);
    }

    [Theory]
    [InlineData(RestoreBehavior.Default)]
    [InlineData(RestoreBehavior.SkipInitialValue)]
    [InlineData(RestoreBehavior.SkipLastSnapshot)]
    public void ShouldRestore_ValueUpdateContext_WithAllowUpdates(RestoreBehavior behavior)
    {
        var options = new RestoreOptions { AllowUpdates = true, RestoreBehavior = behavior };

        var result = RestoreContext.ValueUpdate.ShouldRestore(options);

        Assert.True(result);
    }
}
