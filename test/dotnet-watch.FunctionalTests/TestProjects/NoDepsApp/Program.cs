// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Started");
            Console.WriteLine($"PID = " + Process.GetCurrentProcess().Id);
            if (args.Length > 0 && args[0] == "--no-exit")
            {
                Thread.Sleep(Timeout.Infinite);
            }
            Console.WriteLine("Exiting");
        }
    }
}
