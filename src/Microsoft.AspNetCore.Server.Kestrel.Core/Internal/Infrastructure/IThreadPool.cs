// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public interface IThreadPool : IScheduler
    {
        void Complete(TaskCompletionSource<object> tcs);
        void Cancel(TaskCompletionSource<object> tcs);
        void Error(TaskCompletionSource<object> tcs, Exception ex);
        void Run(Action action);
        void UnsafeRun(WaitCallback action, object state);
    }
}