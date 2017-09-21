// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Internal
{
    public class ProcessLifetime : IHostLifetime
    {
        public void RegisterDelayStartCallback(Action<object> callback, object state)
        {
            // Never delays start.
            callback(state);
        }

        public void RegisterStopCallback(Action<object> callback, object state)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => callback(state);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}