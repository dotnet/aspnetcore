// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Started");
            // Process ID is insufficient because PID's may be reused.
            Console.WriteLine($"Process identifier = {Process.GetCurrentProcess().Id}, {Process.GetCurrentProcess().StartTime:hh:mm:ss.FF}");
            Console.WriteLine("Defined types = " + typeof(Program).GetTypeInfo().Assembly.DefinedTypes.Count());
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
