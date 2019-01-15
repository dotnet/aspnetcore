// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace ThrowingLibrary
{
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
}
