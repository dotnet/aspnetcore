// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class InvokedRenderModes
{
    public InvokedRenderModes(Mode mode)
    {
        Value = mode;
    }

    public Mode Value { get; set; }

    internal enum Mode
    {
        None,
        Server,
        WebAssembly,
        ServerAndWebAssembly
    }
}
