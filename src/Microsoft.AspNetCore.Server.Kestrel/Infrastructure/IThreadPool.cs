// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public interface IThreadPool
    {
        void Complete(TaskCompletionSource<object> tcs);
        void Cancel(TaskCompletionSource<object> tcs);
        void Error(TaskCompletionSource<object> tcs, Exception ex);
        void Run(Action action);
    }
}