// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ClassLibraryWithPortablePdbs
{
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
}
