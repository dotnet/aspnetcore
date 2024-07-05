// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.InternalTesting;

public class RepeatContext
{
    private static readonly AsyncLocal<RepeatContext> s_current = new AsyncLocal<RepeatContext>();

    public static RepeatContext Current
    {
        get => s_current.Value;
        internal set => s_current.Value = value;
    }

    public RepeatContext(int limit)
    {
        Limit = limit;
    }

    public int Limit { get; }

    public int CurrentIteration { get; set; }
}
