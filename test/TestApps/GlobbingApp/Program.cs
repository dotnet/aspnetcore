// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ConsoleApplication
{
    public class Program
    {
        private static readonly int processId = Process.GetCurrentProcess().Id;

        public static void Main(string[] args)
        {
            ConsoleWrite("GlobbingApp started.");

            File.AppendAllLines(args[0], new string[] { $"{processId}" });
            
            File.WriteAllText(args[0] + ".started", "");
            Block();
        }

        private static void ConsoleWrite(string text)
        {
            Console.WriteLine($"[{processId}] {text}");
        }

        private static void Block()
        {
            while (true)
            {
                ConsoleWrite("Blocked...");
                Thread.Sleep(1000);
            }
        }
    }
}
