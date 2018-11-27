// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Owin.Hosting;
using System;
using System.Threading;

namespace SelfHost
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:42524"))
            {
                Console.WriteLine("Server running at http://localhost:42524/");
                Thread.Sleep(args.Length > 0 ? int.Parse(args[0]) : Timeout.Infinite);
            }
        }
    }
}
