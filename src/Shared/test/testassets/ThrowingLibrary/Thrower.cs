// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace ThrowingLibrary;

// Throwing an exception in the current assembly always seems to populate the full stack
// trace regardless of symbol type. This type exists to simulate an exception thrown
// across assemblies which is the typical use case for StackTraceHelper.
public static class Thrower
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw()
    {
        throw new DivideByZeroException();
    }
}
