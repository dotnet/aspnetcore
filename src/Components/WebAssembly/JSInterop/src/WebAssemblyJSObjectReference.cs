// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.WebAssembly;

internal sealed class WebAssemblyJSObjectReference : JSInProcessObjectReference
{
    private readonly WebAssemblyJSRuntime _jsRuntime;

    public WebAssemblyJSObjectReference(WebAssemblyJSRuntime jsRuntime, long id)
        : base(jsRuntime, id)
    {
        _jsRuntime = jsRuntime;
    }
}
