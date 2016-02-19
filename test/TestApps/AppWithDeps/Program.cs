// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("AppWithDeps started.");

            var processId = Process.GetCurrentProcess().Id;
            File.AppendAllLines(args[0], new string[] { $"{processId}" });

            File.WriteAllText(args[0] + ".started", "");
            Console.ReadLine();
        }
    }
}
