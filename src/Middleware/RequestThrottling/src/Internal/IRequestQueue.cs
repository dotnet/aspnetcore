// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    interface IRequestQueue : IDisposable
    {
        int TotalRequests { get; }

        Task<bool> TryEnterQueueAsync();

        void Release();
    }
}
