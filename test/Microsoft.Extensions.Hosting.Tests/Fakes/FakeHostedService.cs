// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Tests.Fakes
{
    public class FakeHostedService : IHostedService, IDisposable
    {
        public int StartCount { get; internal set; }
        public int StopCount { get; internal set; }
        public int DisposeCount { get; internal set; }

        public Action<CancellationToken> StartAction { get; set; }
        public Action<CancellationToken> StopAction { get; set; }
        public Action DisposeAction { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            StartAction?.Invoke(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            StopAction?.Invoke(cancellationToken);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCount++;
            DisposeAction?.Invoke();
        }
    }
}
