// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Http;

internal interface IDebugger
{
    bool IsAttached { get; }
}

internal sealed class DebuggerWrapper : IDebugger
{
    public static readonly DebuggerWrapper Instance = new DebuggerWrapper();

    private DebuggerWrapper() { }

    public bool IsAttached => Debugger.IsAttached;
}
