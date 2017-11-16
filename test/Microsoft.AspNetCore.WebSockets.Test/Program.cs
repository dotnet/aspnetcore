// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461
using System;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("This Program.Main is only here to work around https://github.com/dotnet/sdk/issues/909");
        }
    }
}
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target frameworks need to be updated
#endif
