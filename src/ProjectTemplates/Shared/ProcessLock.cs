// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Templates.Test.Helpers
{
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
}
