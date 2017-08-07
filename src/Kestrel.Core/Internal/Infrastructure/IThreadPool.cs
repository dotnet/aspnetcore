// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public interface IThreadPool : IScheduler
    {
        void Run(Action action);
        void UnsafeRun(WaitCallback action, object state);
    }
}