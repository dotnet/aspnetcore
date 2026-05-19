// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers;

internal sealed class BuffersThrowHelper
{
    public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
    {
        throw GetArgumentOutOfRangeException(argument);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument)
    {
        return new ArgumentOutOfRangeException(GetArgumentName(argument));
    }

    private static string GetArgumentName(ExceptionArgument argument)
    {
        Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument), "The enum value is not defined, please check the ExceptionArgument Enum.");

        return argument.ToString();
    }

    internal enum ExceptionArgument
    {
        length,
    }
}
