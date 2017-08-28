// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Tests.Fakes
{
    public class FakeHostLifetime : IHostLifetime
    {
        public int StartCount { get; internal set; }
        public int StoppingCount { get; internal set; }
        public int StopCount { get; internal set; }

        public Action<Action<object>, object> StartAction { get; set; }
        public Action<Action<object>, object> StoppingAction { get; set; }
        public Action StopAction { get; set; }
        
        public void RegisterDelayStartCallback(Action<object> callback, object state)
        {
            StartCount++;
            StartAction?.Invoke(callback, state);
        }

        public void RegisterStopCallback(Action<object> callback, object state)
        {
            StoppingCount++;
            StoppingAction?.Invoke(callback, state);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            StopAction?.Invoke();
            return Task.CompletedTask;
        }
    }
}
