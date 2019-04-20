// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace KitchenSink
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            Console.WriteLine("PID = " + Process.GetCurrentProcess().Id);
            Console.WriteLine("DOTNET_WATCH = " + Environment.GetEnvironmentVariable("DOTNET_WATCH"));
            Console.WriteLine("DOTNET_WATCH_ITERATION = " + Environment.GetEnvironmentVariable("DOTNET_WATCH_ITERATION"));
        }
    }
}
