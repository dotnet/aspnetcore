// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

// Represents the renderer ID for a web renderer.
// This should be kept in sync with WebRendererId.ts.
internal enum WebRendererId
{
    Default = 0,
    Server = 1,
    WebAssembly = 2,
    WebView = 3,
}
