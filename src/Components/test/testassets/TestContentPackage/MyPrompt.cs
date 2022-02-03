// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace TestContentPackage;

public static class MyPrompt
{
    public static ValueTask<string> Show(IJSRuntime jsRuntime, string message)
    {
        return jsRuntime.InvokeAsync<string>(
            "TestContentPackage.showPrompt", // Keep in sync with identifiers in the.js file
            message);
    }
}
