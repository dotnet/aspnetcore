// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Wasm.Performance.TestApp;

public static class BenchmarkEvent
{
    public static void Send(IJSRuntime jsRuntime, string name)
    {
        // jsRuntime will be null if we're in an environment without any
        // JS runtime, e.g., the console runner
        ((IJSInProcessRuntime)jsRuntime)?.Invoke<object>(
            "receiveBenchmarkEvent",
            name);
    }
}
