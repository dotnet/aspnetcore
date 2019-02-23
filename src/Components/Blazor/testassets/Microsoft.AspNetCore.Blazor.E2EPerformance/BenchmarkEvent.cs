// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.E2EPerformance
{
    public static class BenchmarkEvent
    {
        public static void Send(IJSRuntime jsRuntime, string name)
        {
            ((IJSInProcessRuntime)jsRuntime).Invoke<object>(
                "receiveBenchmarkEvent",
                name);
        }
    }
}
