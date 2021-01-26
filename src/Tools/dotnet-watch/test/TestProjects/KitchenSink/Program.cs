// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace KitchenSink
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            // Process ID is insufficient because PID's may be reused.
            Console.WriteLine($"Process identifier = {Process.GetCurrentProcess().Id}, {Process.GetCurrentProcess().StartTime:hh:mm:ss.FF}");
            Console.WriteLine("DOTNET_WATCH = " + Environment.GetEnvironmentVariable("DOTNET_WATCH"));
            Console.WriteLine("DOTNET_WATCH_ITERATION = " + Environment.GetEnvironmentVariable("DOTNET_WATCH_ITERATION"));

            if (args.Length > 0 && args[0] == "wait")
            {
                Console.WriteLine("Waiting for process to be terminated.");
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
