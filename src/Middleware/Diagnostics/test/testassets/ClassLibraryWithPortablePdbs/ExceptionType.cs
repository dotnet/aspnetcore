// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ClassLibraryWithPortablePdbs;

public class ExceptionType
{
    public static void StaticMethodThatThrows()
    {
        throw new Exception();
    }

    public void MethodThatThrows()
    {
        throw new Exception();
    }
}
