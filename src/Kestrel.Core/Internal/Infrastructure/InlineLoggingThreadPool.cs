// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class InlineLoggingThreadPool : KestrelThreadPool
    {
        private readonly IKestrelTrace _log;

        public InlineLoggingThreadPool(IKestrelTrace log)
        {
            _log = log;
        }

        public override void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Run");
            }
        }

        public override void UnsafeRun(WaitCallback action, object state)
        {
            action(state);
        }

        public override void Schedule<T>(Action<T> action, T state)
        {
            try
            {
                action(state);
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Schedule");
            }
        }
    }
}
