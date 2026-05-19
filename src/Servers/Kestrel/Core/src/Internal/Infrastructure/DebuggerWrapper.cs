// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class DebuggerWrapper : IDebugger
{
    private DebuggerWrapper()
    { }

    public static IDebugger Singleton { get; } = new DebuggerWrapper();

    public bool IsAttached => Debugger.IsAttached;
}
