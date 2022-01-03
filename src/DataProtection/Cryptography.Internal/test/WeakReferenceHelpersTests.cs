// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography;

public class WeakReferenceHelpersTests
{
    [Fact]
    public void GetSharedInstance_ExistingWeakRefHasBeenGCed_CreatesNew()
    {
        // Arrange
        WeakReference<MyDisposable> wrOriginal = new WeakReference<MyDisposable>(null);
        WeakReference<MyDisposable> wr = wrOriginal;
        MyDisposable newInstance = new MyDisposable();

        // Act
        var retVal = WeakReferenceHelpers.GetSharedInstance(ref wr, () => newInstance);

        // Assert
        Assert.NotNull(wr);
        Assert.NotSame(wrOriginal, wr);
        Assert.True(wr.TryGetTarget(out var target));
        Assert.Same(newInstance, target);
        Assert.Same(newInstance, retVal);
        Assert.False(newInstance.HasBeenDisposed);
    }

    [Fact]
    public void GetSharedInstance_ExistingWeakRefIsNull_CreatesNew()
    {
        // Arrange
        WeakReference<MyDisposable> wr = null;
        MyDisposable newInstance = new MyDisposable();

        // Act
        var retVal = WeakReferenceHelpers.GetSharedInstance(ref wr, () => newInstance);

        // Assert
        Assert.NotNull(wr);
        Assert.True(wr.TryGetTarget(out var target));
        Assert.Same(newInstance, target);
        Assert.Same(newInstance, retVal);
        Assert.False(newInstance.HasBeenDisposed);
    }

    [Fact]
    public void GetSharedInstance_ExistingWeakRefIsNull_AnotherThreadCreatesInstanceWhileOurFactoryRuns_ReturnsExistingInstanceAndDisposesNewInstance()
    {
        // Arrange
        WeakReference<MyDisposable> wr = null;
        MyDisposable instanceThatWillBeCreatedFirst = new MyDisposable();
        MyDisposable instanceThatWillBeCreatedSecond = new MyDisposable();

        // Act
        var retVal = WeakReferenceHelpers.GetSharedInstance(ref wr, () =>
        {
            // mimic another thread creating the instance while our factory is being invoked
            WeakReferenceHelpers.GetSharedInstance(ref wr, () => instanceThatWillBeCreatedFirst);
            return instanceThatWillBeCreatedSecond;
        });

        // Assert
        Assert.NotNull(wr);
        Assert.True(wr.TryGetTarget(out var target));
        Assert.Same(instanceThatWillBeCreatedFirst, target);
        Assert.Same(instanceThatWillBeCreatedFirst, retVal);
        Assert.False(instanceThatWillBeCreatedFirst.HasBeenDisposed);
        Assert.True(instanceThatWillBeCreatedSecond.HasBeenDisposed);
    }

    private sealed class MyDisposable : IDisposable
    {
        public bool HasBeenDisposed { get; private set; }

        public void Dispose()
        {
            HasBeenDisposed = true;
        }
    }
}
