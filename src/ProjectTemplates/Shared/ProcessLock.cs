// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Templates.Test.Helpers;

public class ProcessLock
{
    public static readonly ProcessLock DotNetNewLock = new ProcessLock("dotnet-new");
    public static readonly ProcessLock NodeLock = new ProcessLock("node");

    public ProcessLock(string name)
    {
        Name = name;
        Semaphore = new SemaphoreSlim(1);
    }

    public string Name { get; }
    private SemaphoreSlim Semaphore { get; }

    public async Task WaitAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(20);
        Assert.True(await Semaphore.WaitAsync(timeout.Value), $"Unable to acquire process lock for process {Name}");
    }

    public void Release()
    {
        Semaphore.Release();
    }
}
