// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace AppSettings
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (WebHost.Start(context => context.Response.WriteAsync("Hello, World!")))
            {
                Console.WriteLine("Running application: Press any key to shutdown...");
                Console.ReadKey();
            }
        }
    }
}
